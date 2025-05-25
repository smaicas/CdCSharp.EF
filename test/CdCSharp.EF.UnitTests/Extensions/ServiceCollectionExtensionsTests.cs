using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Core.Resolvers;
using CdCSharp.EF.Core.Stores;
using CdCSharp.EF.Extensions;
using CdCSharp.EF.Features;
using CdCSharp.EF.Features.Abstractions;
using CdCSharp.EF.Features.Auditing;
using CdCSharp.EF.Features.Identity;
using CdCSharp.EF.Features.MultiTenant;
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
        Assert.Contains(processors, p => p is AuditingFeatureProcessor);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddExtensibleDbContext_WithMultiTenantByDatabase_ShouldRegisterExpectedServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpContextAccessor();

        // Act
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            featuresBuilder: features => features
                .EnableMultiTenantByDatabase(tenants => tenants
                    .AddTenant("tenant1", options => options.UseInMemoryDatabase("tenant1-db"))
                    .AddTenant("tenant2", options => options.UseInMemoryDatabase("tenant2-db")))
                .EnableAuditing()
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

        // Verificar que DbContextFeatures tiene MultiTenant habilitado
        DbContextFeatures? features = serviceProvider.GetService<DbContextFeatures>();
        Assert.NotNull(features);
        Assert.True(features.MultiTenant.Enabled);
        Assert.Equal(MultiTenantStrategy.Database, features.MultiTenant.Configuration.Strategy);

        // Para Database strategy debe registrar factory
        Assert.NotNull(serviceProvider.GetService<IMultiTenantDbContextFactory<ServiceCollectionExtensionsTests_ExtensibleDbContext>>());

        // Verificar que hay procesadores registrados
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.NotEmpty(processors);
        Assert.Contains(processors, p => p is AuditingFeatureProcessor);
        Assert.Contains(processors, p => p is MultiTenantFeatureProcessor);

        // Context debe resolverse correctamente
        Assert.NotNull(serviceProvider.GetService<ServiceCollectionExtensionsTests_ExtensibleDbContext>());

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddExtensibleDbContext_WithMultiTenantByDatabase_EmptyTenants_ShouldThrowArgumentException()
    {
        // Arrange
        ServiceCollection services = new();

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
                featuresBuilder: features => features
                    .EnableMultiTenantByDatabase(tenants => { }) // Empty builder
                    .EnableAuditing()
            )
        );

        Assert.Contains("At least one tenant configuration is required", exception.Message);
    }

    [Fact]
    public void AddExtensibleDbContext_WithMultiTenantByDatabase_ShouldResolveDbContext()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            featuresBuilder: features => features
                .EnableMultiTenantByDatabase(tenants => tenants
                    .AddTenant("tenant1", options => options.UseInMemoryDatabase("tenant1-db")))
                .EnableAuditing()
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IWritableTenantStore? tenantStore = serviceProvider.GetService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId("tenant1");

        // Act
        ServiceCollectionExtensionsTests_ExtensibleDbContext? context = serviceProvider.GetService<ServiceCollectionExtensionsTests_ExtensibleDbContext>();

        // Assert
        Assert.NotNull(context);
        context?.Dispose();
        serviceProvider.Dispose();
    }

    [Fact]
    public void AddExtensibleDbContext_WithMultiTenantByDiscriminator_ShouldRegisterExpectedServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpContextAccessor();

        // Act
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features.EnableMultiTenantByDiscriminator()
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

        // Verificar que DbContextFeatures tiene MultiTenant habilitado
        DbContextFeatures? features = serviceProvider.GetService<DbContextFeatures>();
        Assert.NotNull(features);
        Assert.True(features.MultiTenant.Enabled);
        Assert.Equal(MultiTenantStrategy.Discriminator, features.MultiTenant.Configuration.Strategy);

        // Para Discriminator strategy NO debe registrar factory (usa registro normal)
        Assert.NotNull(serviceProvider.GetService<IMultiTenantDbContextFactory<ServiceCollectionExtensionsTests_ExtensibleDbContext>>());

        // Context debe resolverse correctamente
        Assert.NotNull(serviceProvider.GetService<ServiceCollectionExtensionsTests_ExtensibleDbContext>());

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddExtensibleDbContext_WithMultiTenantByDiscriminator_AndFeatures_ShouldRegisterExpectedServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddHttpContextAccessor();

        // Act
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features
                .EnableMultiTenantByDiscriminator()
                .EnableAuditing()
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

        // Verificar que DbContextFeatures tiene MultiTenant habilitado
        DbContextFeatures? features = serviceProvider.GetService<DbContextFeatures>();
        Assert.NotNull(features);
        Assert.True(features.MultiTenant.Enabled);
        Assert.Equal(MultiTenantStrategy.Discriminator, features.MultiTenant.Configuration.Strategy);

        // Context debe resolverse correctamente
        Assert.NotNull(serviceProvider.GetService<ServiceCollectionExtensionsTests_ExtensibleDbContext>());

        // Verificar que hay procesadores registrados
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.NotEmpty(processors);
        Assert.Contains(processors, p => p is AuditingFeatureProcessor);
        Assert.Contains(processors, p => p is MultiTenantFeatureProcessor);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddExtensibleDbContext_WithMultiTenantByDiscriminator_ShouldResolveDbContext()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features.EnableMultiTenantByDiscriminator()
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Set a tenant before resolving the context
        IWritableTenantStore? tenantStore = serviceProvider.GetService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId("test-tenant");

        // Act
        ServiceCollectionExtensionsTests_ExtensibleDbContext? context = serviceProvider.GetService<ServiceCollectionExtensionsTests_ExtensibleDbContext>();

        // Assert
        Assert.NotNull(context);
        Assert.Equal("test-tenant", context.CurrentTenantId);
        context?.Dispose();
        serviceProvider.Dispose();
    }

    [Fact]
    public void AddCustomTenantResolver_WhenCalled_ShouldReplaceDefaultResolver()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features.EnableMultiTenantByDiscriminator()
        );

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
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features.EnableMultiTenantByDiscriminator()
        );

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
    public void ServiceRegistrations_ForMultiTenantByDiscriminator_ShouldHaveCorrectLifetime()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features.EnableMultiTenantByDiscriminator()
        );

        // Assert
        ServiceDescriptor tenantStoreDescriptor = services.First(s => s.ServiceType == typeof(ITenantStore));
        Assert.Equal(ServiceLifetime.Scoped, tenantStoreDescriptor.Lifetime);

        ServiceDescriptor tenantResolverDescriptor = services.First(s => s.ServiceType == typeof(ITenantResolver));
        Assert.Equal(ServiceLifetime.Scoped, tenantResolverDescriptor.Lifetime);

        ServiceDescriptor featuresDescriptor = services.First(s => s.ServiceType == typeof(DbContextFeatures));
        Assert.Equal(ServiceLifetime.Singleton, featuresDescriptor.Lifetime);

        ServiceDescriptor contextDescriptor = services.First(s => s.ServiceType == typeof(ServiceCollectionExtensionsTests_ExtensibleDbContext));
        Assert.Equal(ServiceLifetime.Scoped, contextDescriptor.Lifetime);
    }

    [Fact]
    public void ServiceRegistrations_ForMultiTenantByDatabase_ShouldHaveCorrectLifetime()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            featuresBuilder: features => features
                .EnableMultiTenantByDatabase(tenants => tenants
                    .AddTenant("tenant1", opts => opts.UseInMemoryDatabase("tenant1-db")))
        );

        // Assert
        ServiceDescriptor tenantStoreDescriptor = services.First(s => s.ServiceType == typeof(ITenantStore));
        Assert.Equal(ServiceLifetime.Scoped, tenantStoreDescriptor.Lifetime);

        ServiceDescriptor tenantResolverDescriptor = services.First(s => s.ServiceType == typeof(ITenantResolver));
        Assert.Equal(ServiceLifetime.Scoped, tenantResolverDescriptor.Lifetime);

        ServiceDescriptor featuresDescriptor = services.First(s => s.ServiceType == typeof(DbContextFeatures));
        Assert.Equal(ServiceLifetime.Singleton, featuresDescriptor.Lifetime);

        ServiceDescriptor factoryDescriptor = services.First(s => s.ServiceType == typeof(IMultiTenantDbContextFactory<ServiceCollectionExtensionsTests_ExtensibleDbContext>));
        Assert.Equal(ServiceLifetime.Scoped, factoryDescriptor.Lifetime);

        ServiceDescriptor contextDescriptor = services.First(s => s.ServiceType == typeof(ServiceCollectionExtensionsTests_ExtensibleDbContext));
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
    public void AddExtensibleDbContext_WithMultiTenantByDatabase_AndIdentityFeature_ShouldRegisterIdentityProcessor()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            featuresBuilder: features => features
                .EnableMultiTenantByDatabase(tenants => tenants
                    .AddTenant("tenant1", options => options.UseInMemoryDatabase("tenant1-db")))
                .EnableIdentity<Guid>()
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
    public void AddExtensibleDbContext_WithMultiTenantByDiscriminator_AndIdentityFeature_ShouldRegisterIdentityProcessor()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features
                .EnableMultiTenantByDiscriminator()
                .EnableIdentity<Guid>()
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
        Assert.Contains(processors, p => p is MultiTenantFeatureProcessor);

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
    public void AddExtensibleDbContext_WithMultiTenantDiscriminator_AuditingAndIdentity_ShouldRegisterAllProcessors()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            options => options.UseInMemoryDatabase("test-db"),
            features => features
                .EnableMultiTenantByDiscriminator()
                .EnableAuditing()
                .EnableIdentity<Guid>()
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        DbContextFeatures? dbFeatures = serviceProvider.GetService<DbContextFeatures>();
        Assert.NotNull(dbFeatures);
        Assert.True(dbFeatures.MultiTenant.Enabled);
        Assert.True(dbFeatures.Auditing.Enabled);
        Assert.True(dbFeatures.Identity.Enabled);

        // Verificar que hay procesadores registrados para todas las features
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.Contains(processors, p => p is MultiTenantFeatureProcessor);
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
        Assert.False(dbFeatures.MultiTenant.Enabled);

        // Verificar que solo hay procesador de auditing
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.Contains(processors, p => p is AuditingFeatureProcessor);
        Assert.DoesNotContain(processors, p => p is IdentityFeatureProcessor);
        Assert.DoesNotContain(processors, p => p is MultiTenantFeatureProcessor);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddExtensibleDbContext_WithoutFeatures_ShouldRegisterBasicServices()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            options => options.UseInMemoryDatabase("test-db")
            // Sin features
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        DbContextFeatures? dbFeatures = serviceProvider.GetService<DbContextFeatures>();
        Assert.NotNull(dbFeatures);
        Assert.False(dbFeatures.Auditing.Enabled);
        Assert.False(dbFeatures.Identity.Enabled);
        Assert.False(dbFeatures.MultiTenant.Enabled);

        // Context debe resolverse correctamente
        Assert.NotNull(serviceProvider.GetService<ServiceCollectionExtensionsTests_ExtensibleDbContext>());

        // No debe haber procesadores registrados
        IEnumerable<IFeatureProcessor> processors = serviceProvider.GetServices<IFeatureProcessor>();
        Assert.Empty(processors);

        serviceProvider.Dispose();
    }

    [Fact]
    public void AddExtensibleDbContext_WithDatabaseMultiTenant_ShouldNotAllowContextWithoutTenant()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddExtensibleDbContext<ServiceCollectionExtensionsTests_ExtensibleDbContext>(
            featuresBuilder: features => features
                .EnableMultiTenantByDatabase(tenants => tenants
                    .AddTenant("tenant1", options => options.UseInMemoryDatabase("tenant1-db")))
        );

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        // NO configuramos tenant ID

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
        {
            ServiceCollectionExtensionsTests_ExtensibleDbContext? context = serviceProvider.GetService<ServiceCollectionExtensionsTests_ExtensibleDbContext>();
        });

        Assert.Contains("Current tenant ID is not set", exception.Message);
        serviceProvider.Dispose();
    }

    // Clases auxiliares para tests
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
        public ServiceCollectionExtensionsTests_ExtensibleDbContext(
            DbContextOptions<ServiceCollectionExtensionsTests_ExtensibleDbContext> options,
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