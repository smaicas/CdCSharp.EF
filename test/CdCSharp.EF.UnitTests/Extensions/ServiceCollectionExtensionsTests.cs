using CdCSharp.EF.Configuration;
using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Core.Resolvers;
using CdCSharp.EF.Core.Stores;
using CdCSharp.EF.Extensions;
using CdCSharp.EF.Features;
using CdCSharp.EF.Features.Abstractions;
using CdCSharp.EF.Features.Auditing;
using CdCSharp.EF.Features.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddExtensibleDbContext_WithFeatures_ShouldRegisterExpectedServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpContextAccessor();

        // Act
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features.EnableAuditing()
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<DbContextFeatures>());
        Assert.NotNull(serviceProvider.GetService<ICurrentUserStore>());
        Assert.NotNull(serviceProvider.GetService<ICurrentUserResolver>());
        Assert.NotNull(serviceProvider.GetService<ServiceCollectionExtensionsTests_ExtensibleDbContext>());

        // Verificar que hay al menos un procesador registrado
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.NotEmpty(processors);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddMultiTenantByDatabaseDbContext_WithValidBuilder_ShouldRegisterExpectedServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpContextAccessor();

        // Act
        services.AddMultiTenantByDatabaseDbContext<ServiceCollectionExtensionsTests_MultiTenantDbContext>(
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
        Assert.NotNull(serviceProvider.GetService<MultiTenantConfiguration<ServiceCollectionExtensionsTests_MultiTenantDbContext>>());

        // Verificar configuración multi-tenant
        MultiTenantConfiguration<ServiceCollectionExtensionsTests_MultiTenantDbContext>? configuration = serviceProvider.GetService<MultiTenantConfiguration<ServiceCollectionExtensionsTests_MultiTenantDbContext>>();
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
    public void AddMultiTenantByDatabaseDbContext_WithEmptyTenants_ShouldThrowArgumentException()
    {
        // Arrange
        ServiceCollection services = new();

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            services.AddMultiTenantByDatabaseDbContext<ServiceCollectionExtensionsTests_MultiTenantDbContext>(
                tenants => { }, // Empty builder
                features => features.EnableAuditing()
            )
        );

        Assert.Contains("At least one tenant configuration is required", exception.Message);
    }

    [Fact]
    public void AddMultiTenantByDatabaseDbContext_WithValidTenant_ShouldResolveDbContext()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMultiTenantByDatabaseDbContext<ServiceCollectionExtensionsTests_MultiTenantDbContext>(
            tenants => tenants
        .AddTenant("tenant1", options => options.UseInMemoryDatabase("tenant1-db")),
            features => features.EnableAuditing()
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IWritableTenantStore? tenantStore = serviceProvider.GetService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId("tenant1");

        // Act
        ServiceCollectionExtensionsTests_MultiTenantDbContext? context = serviceProvider.GetService<ServiceCollectionExtensionsTests_MultiTenantDbContext>();

        // Assert
        Assert.NotNull(context);
        context?.Dispose();
        serviceProvider.Dispose();
    }

    [Fact]
    public void AddMultiTenantByDiscriminatorDbContext_WithValidOptions_ShouldRegisterExpectedServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpContextAccessor();

        // Act
        services.AddMultiTenantByDiscriminatorDbContext<ServiceCollectionExtensionsTests_MultiTenantDbContext>(options =>
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

        Assert.NotNull(serviceProvider.GetService<MultiTenantConfiguration<ServiceCollectionExtensionsTests_MultiTenantDbContext>>());
        Assert.NotNull(serviceProvider.GetService<IMultiTenantDbContextFactory<ServiceCollectionExtensionsTests_MultiTenantDbContext>>());

        // Now we can safely resolve the context
        Assert.NotNull(serviceProvider.GetService<ServiceCollectionExtensionsTests_MultiTenantDbContext>());

        MultiTenantConfiguration<ServiceCollectionExtensionsTests_MultiTenantDbContext>? configuration = serviceProvider.GetService<MultiTenantConfiguration<ServiceCollectionExtensionsTests_MultiTenantDbContext>>();
        Assert.Equal(MultiTenantStrategy.Discriminator, configuration!.Strategy);
        Assert.NotNull(configuration.DiscriminatorConfiguration);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddMultiTenantByDiscriminatorDbContext_WithFeatures_ShouldRegisterExpectedServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpContextAccessor();

        // Act
        services.AddMultiTenantByDiscriminatorDbContext<ServiceCollectionExtensionsTests_MultiTenantDbContext>(
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
        Assert.NotNull(serviceProvider.GetService<MultiTenantConfiguration<ServiceCollectionExtensionsTests_MultiTenantDbContext>>());
        Assert.NotNull(serviceProvider.GetService<IMultiTenantDbContextFactory<ServiceCollectionExtensionsTests_MultiTenantDbContext>>());

        // Now we can safely resolve the context
        Assert.NotNull(serviceProvider.GetService<ServiceCollectionExtensionsTests_MultiTenantDbContext>());

        MultiTenantConfiguration<ServiceCollectionExtensionsTests_MultiTenantDbContext>? configuration = serviceProvider.GetService<MultiTenantConfiguration<ServiceCollectionExtensionsTests_MultiTenantDbContext>>();
        Assert.Equal(MultiTenantStrategy.Discriminator, configuration!.Strategy);
        Assert.NotNull(configuration.DiscriminatorConfiguration);

        // Verificar que hay procesadores registrados
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.NotEmpty(processors);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddCustomTenantResolver_WhenCalled_ShouldReplaceDefaultResolver()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMultiTenantByDiscriminatorDbContext<ServiceCollectionExtensionsTests_MultiTenantDbContext>(options =>
            options.UseInMemoryDatabase("test-db"));

        // Act
        services.AddCustomTenantResolver<ServiceCollectionExtensionsTests_TenantResolver>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        ITenantResolver? resolver = serviceProvider.GetService<ITenantResolver>();
        Assert.NotNull(resolver);
        Assert.IsType<ServiceCollectionExtensionsTests_TenantResolver>(resolver);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddCustomTenantStore_WhenCalled_ShouldReplaceDefaultStore()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMultiTenantByDiscriminatorDbContext<ServiceCollectionExtensionsTests_MultiTenantDbContext>(options =>
            options.UseInMemoryDatabase("test-db"));

        // Act
        services.AddCustomTenantStore<ServiceCollectionExtensionsTests_TenantStore>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        ITenantStore? store = serviceProvider.GetService<ITenantStore>();
        Assert.NotNull(store);
        Assert.IsType<ServiceCollectionExtensionsTests_TenantStore>(store);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddMultiTenantByDiscriminatorDbContext_WithValidTenant_ShouldResolveDbContext()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMultiTenantByDiscriminatorDbContext<ServiceCollectionExtensionsTests_MultiTenantDbContext>(options =>
            options.UseInMemoryDatabase("test-db"));
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Set a tenant before resolving the context
        IWritableTenantStore? tenantStore = serviceProvider.GetService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId("test-tenant");

        // Act
        ServiceCollectionExtensionsTests_MultiTenantDbContext? context = serviceProvider.GetService<ServiceCollectionExtensionsTests_MultiTenantDbContext>();

        // Assert
        Assert.NotNull(context);
        context?.Dispose();
        serviceProvider.Dispose();
    }

    [Fact]
    public void ServiceRegistrations_ForMultiTenantByDiscriminator_ShouldHaveCorrectLifetime()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddMultiTenantByDiscriminatorDbContext<ServiceCollectionExtensionsTests_MultiTenantDbContext>(options =>
            options.UseInMemoryDatabase("test-db"));

        // Assert
        ServiceDescriptor tenantStoreDescriptor = services.First(s => s.ServiceType == typeof(ITenantStore));
        Assert.Equal(ServiceLifetime.Scoped, tenantStoreDescriptor.Lifetime);

        ServiceDescriptor tenantResolverDescriptor = services.First(s => s.ServiceType == typeof(ITenantResolver));
        Assert.Equal(ServiceLifetime.Scoped, tenantResolverDescriptor.Lifetime);

        ServiceDescriptor configurationDescriptor = services.First(s => s.ServiceType == typeof(MultiTenantConfiguration<ServiceCollectionExtensionsTests_MultiTenantDbContext>));
        Assert.Equal(ServiceLifetime.Singleton, configurationDescriptor.Lifetime);

        ServiceDescriptor factoryDescriptor = services.First(s => s.ServiceType == typeof(IMultiTenantDbContextFactory<ServiceCollectionExtensionsTests_MultiTenantDbContext>));
        Assert.Equal(ServiceLifetime.Scoped, factoryDescriptor.Lifetime);

        ServiceDescriptor contextDescriptor = services.First(s => s.ServiceType == typeof(ServiceCollectionExtensionsTests_MultiTenantDbContext));
        Assert.Equal(ServiceLifetime.Scoped, contextDescriptor.Lifetime);
    }

    [Fact]
    public void AddExtensibleDbContext_WithIdentityFeature_ShouldRegisterIdentityProcessor()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features.EnableIdentity<Guid>()
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        DbContextFeatures? dbFeatures = serviceProvider.GetService<DbContextFeatures>();
        Assert.NotNull(dbFeatures);
        Assert.True(dbFeatures.Identity.Enabled);

        // Verificar que hay procesadores registrados incluyendo Identity
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.Contains(processors, p => p is IdentityFeatureProcessor);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddMultiTenantByDatabaseDbContext_WithIdentityFeature_ShouldRegisterIdentityProcessor()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddMultiTenantByDatabaseDbContext<ServiceCollectionExtensionsTests_MultiTenantDbContext>(
            tenants => tenants
                .AddTenant("tenant1", options => options.UseInMemoryDatabase("tenant1-db")),
            features => features.EnableIdentity<Guid>()
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Set a tenant before checking features
        IWritableTenantStore? tenantStore = serviceProvider.GetRequiredService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId("tenant1");

        // Assert
        DbContextFeatures? dbFeatures = serviceProvider.GetService<DbContextFeatures>();
        Assert.NotNull(dbFeatures);
        Assert.True(dbFeatures.Identity.Enabled);

        // Verificar que hay procesadores registrados incluyendo Identity
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.Contains(processors, p => p is IdentityFeatureProcessor);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddMultiTenantByDiscriminatorDbContext_WithIdentityFeature_ShouldRegisterIdentityProcessor()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddMultiTenantByDiscriminatorDbContext<ServiceCollectionExtensionsTests_MultiTenantDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features.EnableIdentity<Guid>()
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Set a tenant before checking features
        IWritableTenantStore? tenantStore = serviceProvider.GetRequiredService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId("test-tenant");

        // Assert
        DbContextFeatures? dbFeatures = serviceProvider.GetService<DbContextFeatures>();
        Assert.NotNull(dbFeatures);
        Assert.True(dbFeatures.Identity.Enabled);

        // Verificar que hay procesadores registrados incluyendo Identity
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.Contains(processors, p => p is IdentityFeatureProcessor);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddExtensibleDbContext_WithBothAuditingAndIdentity_ShouldRegisterBothProcessors()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features
                .EnableAuditing()
                .EnableIdentity<Guid>()
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        DbContextFeatures? dbFeatures = serviceProvider.GetService<DbContextFeatures>();
        Assert.NotNull(dbFeatures);
        Assert.True(dbFeatures.Auditing.Enabled);
        Assert.True(dbFeatures.Identity.Enabled);

        // Verificar que hay procesadores registrados para ambas features
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.Contains(processors, p => p is AuditingFeatureProcessor);
        Assert.Contains(processors, p => p is IdentityFeatureProcessor);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddExtensibleDbContext_WithIdentityDisabled_ShouldNotRegisterIdentityProcessor()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features.EnableAuditing() // Solo auditing, no identity
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        DbContextFeatures? dbFeatures = serviceProvider.GetService<DbContextFeatures>();
        Assert.NotNull(dbFeatures);
        Assert.True(dbFeatures.Auditing.Enabled);
        Assert.False(dbFeatures.Identity.Enabled);

        // Verificar que solo hay procesador de auditing
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.Contains(processors, p => p is AuditingFeatureProcessor);
        Assert.DoesNotContain(processors, p => p is IdentityFeatureProcessor);

        serviceProvider.Dispose();
    }

    internal class ServiceCollectionExtensionsTests_TenantResolver : ITenantResolver
    {
        public Task<string?> ResolveTenantIdAsync() => Task.FromResult<string?>("custom-tenant");
    }

    internal class ServiceCollectionExtensionsTests_TenantStore : ITenantStore
    {
        private string? _tenantId;

        public string? GetCurrentTenantId() => _tenantId;
        public void SetCurrentTenantId(string tenantId) => _tenantId = tenantId;
        public void ClearCurrentTenantId() => _tenantId = null;
    }

    internal class ServiceCollectionExtensionsTests_ExtensibleDbContext : ExtensibleDbContext
    {
        public ServiceCollectionExtensionsTests_ExtensibleDbContext(DbContextOptions<ServiceCollectionExtensionsTests_ExtensibleDbContext> options,
            IServiceProvider serviceProvider)
            : base(options, serviceProvider)
        {
        }

        public DbSet<ServiceCollectionExtensionsTests_TenantEntity> Products { get; set; } = null!;
    }

    internal class ServiceCollectionExtensionsTests_MultiTenantDbContext : MultiTenantDbContext
    {
        public ServiceCollectionExtensionsTests_MultiTenantDbContext(DbContextOptions<ServiceCollectionExtensionsTests_MultiTenantDbContext> options,
            IServiceProvider serviceProvider)
            : base(options, serviceProvider)
        {
        }

        public DbSet<ServiceCollectionExtensionsTests_TenantEntity> Products { get; set; } = null!;
    }

    internal class ServiceCollectionExtensionsTests_TenantEntity : ITenantEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string TenantId { get; set; } = string.Empty;
    }
}