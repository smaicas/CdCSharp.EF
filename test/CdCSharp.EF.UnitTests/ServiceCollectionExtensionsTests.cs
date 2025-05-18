using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Core.Resolvers;
using CdCSharp.EF.Core.Stores;
using CdCSharp.EF.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.UnitTests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMultiTenantByDiscriminatorDbContext_RegistersExpectedServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpContextAccessor();

        // Act
        services.AddMultiTenantByDiscriminatorDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("test-db"));

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Set a tenant before resolving the context
        IWritableTenantStore? tenantStore = serviceProvider.GetRequiredService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId("test-tenant");

        // Assert
        Assert.NotNull(serviceProvider.GetService<ITenantStore>());
        Assert.IsType<InMemoryTenantStore>(serviceProvider.GetService<ITenantStore>());

        Assert.NotNull(serviceProvider.GetService<ITenantResolver>());
        Assert.IsType<HttpHeaderTenantResolver>(serviceProvider.GetService<ITenantResolver>());

        Assert.NotNull(serviceProvider.GetService<MultiTenantConfiguration<TestDbContext>>());
        Assert.NotNull(serviceProvider.GetService<IMultiTenantDbContextFactory<TestDbContext>>());

        // Now we can safely resolve the context
        Assert.NotNull(serviceProvider.GetService<TestDbContext>());

        MultiTenantConfiguration<TestDbContext>? configuration = serviceProvider.GetService<MultiTenantConfiguration<TestDbContext>>();
        Assert.Equal(MultiTenantStrategy.Discriminator, configuration!.Strategy);
        Assert.NotNull(configuration.DiscriminatorConfiguration);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddMultiTenantByDatabaseDbContext_FirstCall_RegistersBaseServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpContextAccessor();

        // Act
        services.AddMultiTenantByDatabaseDbContext<TestDbContext>("tenant1", options =>
            options.UseInMemoryDatabase("tenant1-db"));

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<ITenantStore>());
        Assert.IsType<InMemoryTenantStore>(serviceProvider.GetService<ITenantStore>());

        Assert.NotNull(serviceProvider.GetService<ITenantResolver>());
        Assert.IsType<HttpHeaderTenantResolver>(serviceProvider.GetService<ITenantResolver>());

        MultiTenantConfiguration<TestDbContext>? configuration = serviceProvider.GetService<MultiTenantConfiguration<TestDbContext>>();
        Assert.NotNull(configuration);
        Assert.Equal(MultiTenantStrategy.Database, configuration.Strategy);
        Assert.Single(configuration.DatabaseConfigurations);
        Assert.True(configuration.DatabaseConfigurations.ContainsKey("tenant1"));

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddMultiTenantByDatabaseDbContext_MultipleCalls_AddsConfigurationsToSameInstance()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddMultiTenantByDatabaseDbContext<TestDbContext>("tenant1", options =>
            options.UseInMemoryDatabase("tenant1-db"));

        services.AddMultiTenantByDatabaseDbContext<TestDbContext>("tenant2", options =>
            options.UseInMemoryDatabase("tenant2-db"));

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        MultiTenantConfiguration<TestDbContext>? configuration = serviceProvider.GetService<MultiTenantConfiguration<TestDbContext>>();
        Assert.NotNull(configuration);
        Assert.Equal(MultiTenantStrategy.Database, configuration.Strategy);
        Assert.Equal(2, configuration.DatabaseConfigurations.Count);
        Assert.True(configuration.DatabaseConfigurations.ContainsKey("tenant1"));
        Assert.True(configuration.DatabaseConfigurations.ContainsKey("tenant2"));

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddCustomTenantResolver_ReplacesDefaultResolver()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMultiTenantByDiscriminatorDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("test-db"));

        // Act
        services.AddCustomTenantResolver<CustomTenantResolver>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        ITenantResolver? resolver = serviceProvider.GetService<ITenantResolver>();
        Assert.NotNull(resolver);
        Assert.IsType<CustomTenantResolver>(resolver);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddCustomTenantStore_ReplacesDefaultStore()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMultiTenantByDiscriminatorDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("test-db"));

        // Act
        services.AddCustomTenantStore<CustomTenantStore>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        ITenantStore? store = serviceProvider.GetService<ITenantStore>();
        Assert.NotNull(store);
        Assert.IsType<CustomTenantStore>(store);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddMultiTenantByDiscriminatorDbContext_CanResolveDbContext()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMultiTenantByDiscriminatorDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("test-db"));
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Set a tenant before resolving the context
        IWritableTenantStore? tenantStore = serviceProvider.GetService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId("test-tenant");

        // Act
        TestDbContext? context = serviceProvider.GetService<TestDbContext>();

        // Assert
        Assert.NotNull(context);
        context?.Dispose();
        serviceProvider.Dispose();
    }

    [Fact]
    public void AddMultiTenantByDatabaseDbContext_CanResolveDbContext()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMultiTenantByDatabaseDbContext<TestDbContext>("tenant1", options =>
            options.UseInMemoryDatabase("tenant1-db"));

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IWritableTenantStore? tenantStore = serviceProvider.GetService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId("tenant1");

        // Act
        TestDbContext? context = serviceProvider.GetService<TestDbContext>();

        // Assert
        Assert.NotNull(context);
        context?.Dispose();
        serviceProvider.Dispose();
    }

    [Fact]
    public void ServiceRegistrations_HaveCorrectLifetime()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddMultiTenantByDiscriminatorDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("test-db"));

        // Assert
        ServiceDescriptor tenantStoreDescriptor = services.First(s => s.ServiceType == typeof(ITenantStore));
        Assert.Equal(ServiceLifetime.Scoped, tenantStoreDescriptor.Lifetime);

        ServiceDescriptor tenantResolverDescriptor = services.First(s => s.ServiceType == typeof(ITenantResolver));
        Assert.Equal(ServiceLifetime.Scoped, tenantResolverDescriptor.Lifetime);

        ServiceDescriptor configurationDescriptor = services.First(s => s.ServiceType == typeof(MultiTenantConfiguration<TestDbContext>));
        Assert.Equal(ServiceLifetime.Singleton, configurationDescriptor.Lifetime);

        ServiceDescriptor factoryDescriptor = services.First(s => s.ServiceType == typeof(IMultiTenantDbContextFactory<TestDbContext>));
        Assert.Equal(ServiceLifetime.Scoped, factoryDescriptor.Lifetime);

        ServiceDescriptor contextDescriptor = services.First(s => s.ServiceType == typeof(TestDbContext));
        Assert.Equal(ServiceLifetime.Scoped, contextDescriptor.Lifetime);
    }
}

// Test implementations
public class CustomTenantResolver : ITenantResolver
{
    public Task<string?> ResolveTenantIdAsync() => Task.FromResult<string?>("custom-tenant");
}

public class CustomTenantStore : ITenantStore
{
    private string? _tenantId;

    public string? GetCurrentTenantId() => _tenantId;
    public void SetCurrentTenantId(string tenantId) => _tenantId = tenantId;
    public void ClearCurrentTenantId() => _tenantId = null;
}
