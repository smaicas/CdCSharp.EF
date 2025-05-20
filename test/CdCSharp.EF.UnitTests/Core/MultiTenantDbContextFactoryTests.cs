using CdCSharp.EF.Configuration;
using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CdCSharp.EF.UnitTests.Core;

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

        MultiTenantConfiguration<MultiTenantDbContextFactoryTests_MultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Discriminator,
            DiscriminatorConfiguration = options => options.UseInMemoryDatabase("test-db")
        };

        MultiTenantDbContextFactory<MultiTenantDbContextFactoryTests_MultiTenantDbContext> factory = new(
            _serviceProvider, _mockTenantStore.Object, configuration);

        // Act
        using MultiTenantDbContextFactoryTests_MultiTenantDbContext context = factory.CreateDbContext();

        // Assert
        Assert.NotNull(context);
        Assert.Equal(tenantId, context.CurrentTenantId);
    }

    [Fact]
    public void CreateDbContext_WithSpecificTenantId_CreatesContextWithTenantId()
    {
        // Arrange
        const string tenantId = "tenant1";

        MultiTenantConfiguration<MultiTenantDbContextFactoryTests_MultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Discriminator,
            DiscriminatorConfiguration = options => options.UseInMemoryDatabase("test-db")
        };

        MultiTenantDbContextFactory<MultiTenantDbContextFactoryTests_MultiTenantDbContext> factory = new(
            _serviceProvider, _mockTenantStore.Object, configuration);

        // Act
        using MultiTenantDbContextFactoryTests_MultiTenantDbContext context = factory.CreateDbContext(tenantId);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(tenantId, context.CurrentTenantId);
    }

    [Fact]
    public void CreateDbContext_WithDatabaseStrategy_CreatesContextFromConfiguration()
    {
        // Arrange
        const string tenantId = "tenant1";

        MultiTenantConfiguration<MultiTenantDbContextFactoryTests_MultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Database,
            DatabaseConfigurations = new Dictionary<string, Action<DbContextOptionsBuilder<MultiTenantDbContextFactoryTests_MultiTenantDbContext>>>
        {
            {
                tenantId,
                options => options.UseInMemoryDatabase($"tenant-{tenantId}-db")
            }
        }
        };

        MultiTenantDbContextFactory<MultiTenantDbContextFactoryTests_MultiTenantDbContext> factory = new(
            _serviceProvider, _mockTenantStore.Object, configuration);

        // Act
        using MultiTenantDbContextFactoryTests_MultiTenantDbContext context = factory.CreateDbContext(tenantId);

        // Assert
        Assert.NotNull(context);
    }

    [Fact]
    public void CreateDbContext_WithDatabaseStrategyAndMissingConfiguration_ThrowsException()
    {
        // Arrange
        const string tenantId = "missing-tenant";

        MultiTenantConfiguration<MultiTenantDbContextFactoryTests_MultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Database,
            DatabaseConfigurations = new Dictionary<string, Action<DbContextOptionsBuilder<MultiTenantDbContextFactoryTests_MultiTenantDbContext>>>()
        };

        MultiTenantDbContextFactory<MultiTenantDbContextFactoryTests_MultiTenantDbContext> factory = new(
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

        MultiTenantConfiguration<MultiTenantDbContextFactoryTests_MultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Discriminator,
            DiscriminatorConfiguration = options => options.UseInMemoryDatabase("test-db")
        };

        MultiTenantDbContextFactory<MultiTenantDbContextFactoryTests_MultiTenantDbContext> factory = new(
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

        MultiTenantConfiguration<MultiTenantDbContextFactoryTests_MultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Discriminator,
            DiscriminatorConfiguration = options => options.UseInMemoryDatabase("test-db")
        };

        MultiTenantDbContextFactory<MultiTenantDbContextFactoryTests_MultiTenantDbContext> factory = new(
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

        MultiTenantConfiguration<MultiTenantDbContextFactoryTests_MultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Database,
            DatabaseConfigurations = new Dictionary<string, Action<DbContextOptionsBuilder<MultiTenantDbContextFactoryTests_MultiTenantDbContext>>>
        {
            { tenant1, options => options.UseInMemoryDatabase($"db-{tenant1}") },
            { tenant2, options => options.UseInMemoryDatabase($"db-{tenant2}") }
        }
        };

        MultiTenantDbContextFactory<MultiTenantDbContextFactoryTests_MultiTenantDbContext> factory = new(
            _serviceProvider, _mockTenantStore.Object, configuration);

        // Act
        using MultiTenantDbContextFactoryTests_MultiTenantDbContext context1 = factory.CreateDbContext(tenant1);
        using MultiTenantDbContextFactoryTests_MultiTenantDbContext context2 = factory.CreateDbContext(tenant2);

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

        MultiTenantConfiguration<MultiTenantDbContextFactoryTests_MultiTenantDbContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Discriminator,
            DiscriminatorConfiguration = null // Null configuration
        };

        MultiTenantDbContextFactory<MultiTenantDbContextFactoryTests_MultiTenantDbContext> factory = new(
            _serviceProvider, _mockTenantStore.Object, configuration);

        // Act & Assert
        // This should not throw, but the context might not be properly configured
        // The test verifies that null configuration is handled gracefully
        using MultiTenantDbContextFactoryTests_MultiTenantDbContext context = factory.CreateDbContext(tenantId);
        Assert.NotNull(context);
    }

    public void Dispose() => _serviceProvider.Dispose();

    internal class MultiTenantDbContextFactoryTests_MultiTenantDbContext : MultiTenantDbContext
    {
        public MultiTenantDbContextFactoryTests_MultiTenantDbContext(DbContextOptions<MultiTenantDbContextFactoryTests_MultiTenantDbContext> options,
            IServiceProvider serviceProvider)
            : base(options, serviceProvider)
        {
        }

        public DbSet<MultiTenantDbContextFactoryTests_TenantEntity> Products { get; set; } = null!;
    }

    internal class MultiTenantDbContextFactoryTests_TenantEntity : ITenantEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string TenantId { get; set; } = string.Empty;
    }
}
