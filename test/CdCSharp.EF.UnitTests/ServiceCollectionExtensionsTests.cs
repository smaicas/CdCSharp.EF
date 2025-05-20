using CdCSharp.EF.Configuration;
using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Core.Resolvers;
using CdCSharp.EF.Core.Stores;
using CdCSharp.EF.Extensions;
using CdCSharp.EF.Features;
using CdCSharp.EF.Features.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.UnitTests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddExtensibleDbContext_RegistersExpectedServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpContextAccessor();

        // Act
        services.AddExtensibleDbContext<TestExtensibleDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features.EnableAuditing()
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<DbContextFeatures>());
        Assert.NotNull(serviceProvider.GetService<ICurrentUserStore>());
        Assert.NotNull(serviceProvider.GetService<ICurrentUserResolver>());
        Assert.NotNull(serviceProvider.GetService<TestExtensibleDbContext>());

        // Verificar que hay al menos un procesador registrado
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.NotEmpty(processors);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddMultiTenantByDatabaseDbContext_WithBuilder_RegistersExpectedServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpContextAccessor();

        // Act
        services.AddMultiTenantByDatabaseDbContext<TestMultiTenantDbContext>(
            tenants => tenants
                .AddTenant("tenant1", options => options.UseInMemoryDatabase("tenant1-db"))
                .AddTenant("tenant2", options => options.UseInMemoryDatabase("tenant2-db")),
            features => features.EnableAuditing()
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Set a tenant before resolving the context
        IWritableTenantStore? tenantStore = serviceProvider.GetRequiredService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId("tenant1");

        // Assert
        Assert.NotNull(serviceProvider.GetService<ITenantStore>());
        Assert.IsType<InMemoryTenantStore>(serviceProvider.GetService<ITenantStore>());

        Assert.NotNull(serviceProvider.GetService<ITenantResolver>());
        Assert.IsType<HttpHeaderTenantResolver>(serviceProvider.GetService<ITenantResolver>());

        Assert.NotNull(serviceProvider.GetService<DbContextFeatures>());
        Assert.NotNull(serviceProvider.GetService<ICurrentUserStore>());
        Assert.NotNull(serviceProvider.GetService<ICurrentUserResolver>());
        Assert.NotNull(serviceProvider.GetService<MultiTenantConfiguration<TestMultiTenantDbContext>>());

        // Verificar configuración multi-tenant
        MultiTenantConfiguration<TestMultiTenantDbContext>? configuration = serviceProvider.GetService<MultiTenantConfiguration<TestMultiTenantDbContext>>();
        Assert.Equal(MultiTenantStrategy.Database, configuration!.Strategy);
        Assert.Equal(2, configuration.DatabaseConfigurations.Count);
        Assert.True(configuration.DatabaseConfigurations.ContainsKey("tenant1"));
        Assert.True(configuration.DatabaseConfigurations.ContainsKey("tenant2"));

        // Verificar que hay procesadores registrados
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.NotEmpty(processors);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddMultiTenantByDatabaseDbContext_EmptyTenants_ThrowsException()
    {
        // Arrange
        ServiceCollection services = new();

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            services.AddMultiTenantByDatabaseDbContext<TestMultiTenantDbContext>(
                tenants => { }, // Empty builder
                features => features.EnableAuditing()
            )
        );

        Assert.Contains("At least one tenant configuration is required", exception.Message);
    }

    [Fact]
    public void AddMultiTenantByDatabaseDbContext_CanResolveDbContext()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMultiTenantByDatabaseDbContext<TestMultiTenantDbContext>(
            tenants => tenants
        .AddTenant("tenant1", options => options.UseInMemoryDatabase("tenant1-db")),
            features => features.EnableAuditing()
);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IWritableTenantStore? tenantStore = serviceProvider.GetService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId("tenant1");

        // Act
        TestMultiTenantDbContext? context = serviceProvider.GetService<TestMultiTenantDbContext>();

        // Assert
        Assert.NotNull(context);
        context?.Dispose();
        serviceProvider.Dispose();
    }

    [Fact]
    public void AddMultiTenantByDiscriminatorDbContext_RegistersExpectedServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpContextAccessor();

        // Act
        services.AddMultiTenantByDiscriminatorDbContext<TestMultiTenantDbContext>(options =>
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

        Assert.NotNull(serviceProvider.GetService<MultiTenantConfiguration<TestMultiTenantDbContext>>());
        Assert.NotNull(serviceProvider.GetService<IMultiTenantDbContextFactory<TestMultiTenantDbContext>>());

        // Now we can safely resolve the context
        Assert.NotNull(serviceProvider.GetService<TestMultiTenantDbContext>());

        MultiTenantConfiguration<TestMultiTenantDbContext>? configuration = serviceProvider.GetService<MultiTenantConfiguration<TestMultiTenantDbContext>>();
        Assert.Equal(MultiTenantStrategy.Discriminator, configuration!.Strategy);
        Assert.NotNull(configuration.DiscriminatorConfiguration);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddMultiTenantByDiscriminatorDbContext_WithFeatures_RegistersExpectedServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpContextAccessor();

        // Act
        services.AddMultiTenantByDiscriminatorDbContext<TestMultiTenantDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features.EnableAuditing()
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Set a tenant before resolving the context
        IWritableTenantStore? tenantStore = serviceProvider.GetRequiredService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId("test-tenant");

        // Assert
        Assert.NotNull(serviceProvider.GetService<ITenantStore>());
        Assert.IsType<InMemoryTenantStore>(serviceProvider.GetService<ITenantStore>());

        Assert.NotNull(serviceProvider.GetService<ITenantResolver>());
        Assert.IsType<HttpHeaderTenantResolver>(serviceProvider.GetService<ITenantResolver>());

        Assert.NotNull(serviceProvider.GetService<DbContextFeatures>());
        Assert.NotNull(serviceProvider.GetService<ICurrentUserStore>());
        Assert.NotNull(serviceProvider.GetService<ICurrentUserResolver>());
        Assert.NotNull(serviceProvider.GetService<MultiTenantConfiguration<TestMultiTenantDbContext>>());
        Assert.NotNull(serviceProvider.GetService<IMultiTenantDbContextFactory<TestMultiTenantDbContext>>());

        // Now we can safely resolve the context
        Assert.NotNull(serviceProvider.GetService<TestMultiTenantDbContext>());

        MultiTenantConfiguration<TestMultiTenantDbContext>? configuration = serviceProvider.GetService<MultiTenantConfiguration<TestMultiTenantDbContext>>();
        Assert.Equal(MultiTenantStrategy.Discriminator, configuration!.Strategy);
        Assert.NotNull(configuration.DiscriminatorConfiguration);

        // Verificar que hay procesadores registrados
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.NotEmpty(processors);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddCustomTenantResolver_ReplacesDefaultResolver()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMultiTenantByDiscriminatorDbContext<TestMultiTenantDbContext>(options =>
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
        services.AddMultiTenantByDiscriminatorDbContext<TestMultiTenantDbContext>(options =>
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
        services.AddMultiTenantByDiscriminatorDbContext<TestMultiTenantDbContext>(options =>
            options.UseInMemoryDatabase("test-db"));
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Set a tenant before resolving the context
        IWritableTenantStore? tenantStore = serviceProvider.GetService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId("test-tenant");

        // Act
        TestMultiTenantDbContext? context = serviceProvider.GetService<TestMultiTenantDbContext>();

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
        services.AddMultiTenantByDiscriminatorDbContext<TestMultiTenantDbContext>(options =>
            options.UseInMemoryDatabase("test-db"));

        // Assert
        ServiceDescriptor tenantStoreDescriptor = services.First(s => s.ServiceType == typeof(ITenantStore));
        Assert.Equal(ServiceLifetime.Scoped, tenantStoreDescriptor.Lifetime);

        ServiceDescriptor tenantResolverDescriptor = services.First(s => s.ServiceType == typeof(ITenantResolver));
        Assert.Equal(ServiceLifetime.Scoped, tenantResolverDescriptor.Lifetime);

        ServiceDescriptor configurationDescriptor = services.First(s => s.ServiceType == typeof(MultiTenantConfiguration<TestMultiTenantDbContext>));
        Assert.Equal(ServiceLifetime.Singleton, configurationDescriptor.Lifetime);

        ServiceDescriptor factoryDescriptor = services.First(s => s.ServiceType == typeof(IMultiTenantDbContextFactory<TestMultiTenantDbContext>));
        Assert.Equal(ServiceLifetime.Scoped, factoryDescriptor.Lifetime);

        ServiceDescriptor contextDescriptor = services.First(s => s.ServiceType == typeof(TestMultiTenantDbContext));
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
