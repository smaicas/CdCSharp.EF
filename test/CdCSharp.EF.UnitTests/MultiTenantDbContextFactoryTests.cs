using CdCSharp.EF.Configuration;
using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CdCSharp.EF.UnitTests;

public class MultiTenantDbContextFactoryTests : IDisposable
{
    private readonly Mock<ITenantStore> _mockTenantStore;
    private readonly ServiceProvider _serviceProvider;

    public MultiTenantDbContextFactoryTests()
    {
        _mockTenantStore = new Mock<ITenantStore>();

        ServiceCollection services = new();
        services.AddSingleton(_mockTenantStore.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void CreateDbContext_WithDiscriminatorStrategy_CreatesContextWithTenantId()
    {
        // Arrange
        const string tenantId = "tenant1";
        _mockTenantStore.Setup(s => s.GetCurrentTenantId()).Returns(tenantId);

        MultiTenantConfiguration<TestMultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Discriminator,
            DiscriminatorConfiguration = options => options.UseInMemoryDatabase("test-db")
        };

        MultiTenantDbContextFactory<TestMultiTenantDbContext> factory = new(
            _serviceProvider, _mockTenantStore.Object, configuration);

        // Act
        using TestMultiTenantDbContext context = factory.CreateDbContext();

        // Assert
        Assert.NotNull(context);
        Assert.Equal(tenantId, context.CurrentTenantId);
    }

    [Fact]
    public void CreateDbContext_WithSpecificTenantId_CreatesContextWithTenantId()
    {
        // Arrange
        const string tenantId = "tenant1";

        MultiTenantConfiguration<TestMultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Discriminator,
            DiscriminatorConfiguration = options => options.UseInMemoryDatabase("test-db")
        };

        MultiTenantDbContextFactory<TestMultiTenantDbContext> factory = new(
            _serviceProvider, _mockTenantStore.Object, configuration);

        // Act
        using TestMultiTenantDbContext context = factory.CreateDbContext(tenantId);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(tenantId, context.CurrentTenantId);
    }

    [Fact]
    public void CreateDbContext_WithDatabaseStrategy_CreatesContextFromConfiguration()
    {
        // Arrange
        const string tenantId = "tenant1";

        MultiTenantConfiguration<TestMultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Database,
            DatabaseConfigurations = new Dictionary<string, Action<DbContextOptionsBuilder<TestMultiTenantDbContext>>>
        {
            {
                tenantId,
                options => options.UseInMemoryDatabase($"tenant-{tenantId}-db")
            }
        }
        };

        MultiTenantDbContextFactory<TestMultiTenantDbContext> factory = new(
            _serviceProvider, _mockTenantStore.Object, configuration);

        // Act
        using TestMultiTenantDbContext context = factory.CreateDbContext(tenantId);

        // Assert
        Assert.NotNull(context);
    }

    [Fact]
    public void CreateDbContext_WithDatabaseStrategyAndMissingConfiguration_ThrowsException()
    {
        // Arrange
        const string tenantId = "missing-tenant";

        MultiTenantConfiguration<TestMultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Database,
            DatabaseConfigurations = new Dictionary<string, Action<DbContextOptionsBuilder<TestMultiTenantDbContext>>>()
        };

        MultiTenantDbContextFactory<TestMultiTenantDbContext> factory = new(
            _serviceProvider, _mockTenantStore.Object, configuration);

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext(tenantId));
        Assert.Contains($"No database configuration found for tenant: {tenantId}", exception.Message);
    }

    [Fact]
    public void CreateDbContext_WithoutSpecifyingTenantAndNoCurrentTenant_ThrowsException()
    {
        // Arrange
        _mockTenantStore.Setup(s => s.GetCurrentTenantId()).Returns((string?)null);

        MultiTenantConfiguration<TestMultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Discriminator,
            DiscriminatorConfiguration = options => options.UseInMemoryDatabase("test-db")
        };

        MultiTenantDbContextFactory<TestMultiTenantDbContext> factory = new(
            _serviceProvider, _mockTenantStore.Object, configuration);

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext());
        Assert.Equal("Current tenant ID is not set", exception.Message);
    }

    [Fact]
    public void CreateDbContext_WithoutSpecifyingTenantAndEmptyCurrentTenant_ThrowsException()
    {
        // Arrange
        _mockTenantStore.Setup(s => s.GetCurrentTenantId()).Returns(string.Empty);

        MultiTenantConfiguration<TestMultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Discriminator,
            DiscriminatorConfiguration = options => options.UseInMemoryDatabase("test-db")
        };

        MultiTenantDbContextFactory<TestMultiTenantDbContext> factory = new(
            _serviceProvider, _mockTenantStore.Object, configuration);

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext());
        Assert.Equal("Current tenant ID is not set", exception.Message);
    }

    [Fact]
    public void CreateDbContext_WithMultipleDatabaseConfigurations_CreatesCorrectContext()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        MultiTenantConfiguration<TestMultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Database,
            DatabaseConfigurations = new Dictionary<string, Action<DbContextOptionsBuilder<TestMultiTenantDbContext>>>
        {
            { tenant1, options => options.UseInMemoryDatabase($"db-{tenant1}") },
            { tenant2, options => options.UseInMemoryDatabase($"db-{tenant2}") }
        }
        };

        MultiTenantDbContextFactory<TestMultiTenantDbContext> factory = new(
            _serviceProvider, _mockTenantStore.Object, configuration);

        // Act
        using TestMultiTenantDbContext context1 = factory.CreateDbContext(tenant1);
        using TestMultiTenantDbContext context2 = factory.CreateDbContext(tenant2);

        // Assert
        Assert.NotNull(context1);
        Assert.NotNull(context2);
        Assert.NotSame(context1, context2);
    }

    [Fact]
    public void CreateDbContext_WithDiscriminatorAndNullConfiguration_CreatesContext()
    {
        // Arrange
        const string tenantId = "tenant1";

        MultiTenantConfiguration<TestMultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Discriminator,
            DiscriminatorConfiguration = null // Null configuration
        };

        MultiTenantDbContextFactory<TestMultiTenantDbContext> factory = new(
            _serviceProvider, _mockTenantStore.Object, configuration);

        // Act & Assert
        // This should not throw, but the context might not be properly configured
        // The test verifies that null configuration is handled gracefully
        using TestMultiTenantDbContext context = factory.CreateDbContext(tenantId);
        Assert.NotNull(context);
    }

    public void Dispose() => _serviceProvider.Dispose();
}
