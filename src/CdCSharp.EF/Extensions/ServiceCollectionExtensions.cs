using CdCSharp.EF.Configuration;
using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Core.Resolvers;
using CdCSharp.EF.Core.Stores;
using CdCSharp.EF.Features;
using CdCSharp.EF.Features.Abstractions;
using CdCSharp.EF.Features.Auditing;
using CdCSharp.EF.Features.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds extensible by features DbContext
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    /// <param name="services"></param>
    /// <param name="configureOptions"></param>
    /// <param name="featuresBuilder"></param>
    /// <returns></returns>
    public static IServiceCollection AddExtensibleDbContext<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions,
        Func<DbContextFeaturesBuilder, DbContextFeaturesBuilder>? featuresBuilder = null)
        where TContext : ExtensibleDbContext
    {
        // Configure features
        DbContextFeatures features = featuresBuilder?.Invoke(new DbContextFeaturesBuilder()).Build() ?? DbContextFeatures.Default;
        services.AddSingleton(features);

        // Register processors based on enabled features
        RegisterFeatureProcessors(services, features);

        // Configure required services
        if (!services.Any(s => s.ServiceType == typeof(ICurrentUserStore)))
        {
            services.AddScoped<ICurrentUserStore, InMemoryCurrentUserStore>();
            services.AddScoped<ICurrentUserResolver, ClaimsCurrentUserResolver>();
        }

        // Configure DBContext
        services.AddDbContext<TContext>(configureOptions);

        return services;
    }

    /// <summary>
    /// Adds Multi-Tenant by discriminator and Extensible DbContext
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    /// <param name="services"></param>
    /// <param name="configureOptions"></param>
    /// <param name="featuresBuilder"></param>
    /// <returns></returns>
    public static IServiceCollection AddMultiTenantByDiscriminatorDbContext<TContext>(
    this IServiceCollection services,
    Action<DbContextOptionsBuilder<TContext>> configureOptions,
    Func<DbContextFeaturesBuilder, DbContextFeaturesBuilder>? featuresBuilder = null)
    where TContext : MultiTenantDbContext
    {
        // Configure features
        DbContextFeatures features = featuresBuilder?.Invoke(new DbContextFeaturesBuilder()).Build() ?? DbContextFeatures.Default;
        services.AddSingleton(features);

        // Register processors based on enabled features
        RegisterFeatureProcessors(services, features);

        // Configure required services
        if (!services.Any(s => s.ServiceType == typeof(ICurrentUserStore)))
        {
            services.AddScoped<ICurrentUserStore, InMemoryCurrentUserStore>();
            services.AddScoped<ICurrentUserResolver, ClaimsCurrentUserResolver>();
        }

        // Configure multi-tenant services
        services.AddScoped<ITenantStore, InMemoryTenantStore>();
        services.AddScoped<ITenantResolver, HttpHeaderTenantResolver>();

        MultiTenantConfiguration<TContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Discriminator,
            DiscriminatorConfiguration = configureOptions
        };

        services.AddSingleton(configuration);
        services.AddScoped<IMultiTenantDbContextFactory<TContext>, MultiTenantDbContextFactory<TContext>>();
        services.AddScoped<TContext>(provider =>
        {
            IMultiTenantDbContextFactory<TContext> factory = provider.GetRequiredService<IMultiTenantDbContextFactory<TContext>>();
            return factory.CreateDbContext();
        });

        return services;
    }

    /// <summary>
    /// Adds Multi-Tenant by database and Extensible DbContext
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    /// <param name="services"></param>
    /// <param name="buildTenants"></param>
    /// <param name="featuresBuilder"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IServiceCollection AddMultiTenantByDatabaseDbContext<TContext>(
        this IServiceCollection services,
        Action<MultiTenantByDatabaseBuilder<TContext>> buildTenants,
        Func<DbContextFeaturesBuilder, DbContextFeaturesBuilder>? featuresBuilder = null)
        where TContext : MultiTenantDbContext
    {
        MultiTenantByDatabaseBuilder<TContext> builder = new();
        buildTenants(builder);
        IDictionary<string, Action<DbContextOptionsBuilder<TContext>>> tenantConfigurations = builder.Build();

        if (!tenantConfigurations.Any())
            throw new ArgumentException("At least one tenant configuration is required.", nameof(buildTenants));

        // Configure features
        DbContextFeatures features = featuresBuilder?.Invoke(new DbContextFeaturesBuilder()).Build() ?? DbContextFeatures.Default;
        services.AddSingleton(features);

        // Register processors based on enabled features
        RegisterFeatureProcessors(services, features);

        // Configure required services
        if (!services.Any(s => s.ServiceType == typeof(ICurrentUserStore)))
        {
            services.AddScoped<ICurrentUserStore, InMemoryCurrentUserStore>();
            services.AddScoped<ICurrentUserResolver, ClaimsCurrentUserResolver>();
        }

        // Configure multi-tenant services
        services.AddScoped<ITenantStore, InMemoryTenantStore>();
        services.AddScoped<ITenantResolver, HttpHeaderTenantResolver>();

        MultiTenantConfiguration<TContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Database
        };

        // Add all tenant configurations
        foreach ((string tenantId, Action<DbContextOptionsBuilder<TContext>> configureOptions) in tenantConfigurations)
        {
            configuration.DatabaseConfigurations[tenantId] = configureOptions;
        }

        services.AddSingleton(configuration);
        services.AddScoped<IMultiTenantDbContextFactory<TContext>, MultiTenantDbContextFactory<TContext>>();
        services.AddScoped<TContext>(provider =>
        {
            IMultiTenantDbContextFactory<TContext> factory = provider.GetRequiredService<IMultiTenantDbContextFactory<TContext>>();
            return factory.CreateDbContext();
        });

        return services;
    }

    private static void RegisterFeatureProcessors(IServiceCollection services, DbContextFeatures features)
    {
        if (features.Auditing.Enabled)
        {
            services.AddScoped<IFeatureProcessor, AuditingFeatureProcessor>(provider =>
                new AuditingFeatureProcessor(features.Auditing.Configuration, provider));
        }

        if (features.Identity.Enabled)
        {
            services.AddScoped<IFeatureProcessor, IdentityFeatureProcessor>(provider =>
                new IdentityFeatureProcessor(features.Identity, provider));
        }
    }

    public static IServiceCollection AddCustomTenantResolver<TResolver>(this IServiceCollection services)
        where TResolver : class, ITenantResolver
    {
        services.AddScoped<ITenantResolver, TResolver>();
        return services;
    }

    public static IServiceCollection AddCustomTenantStore<TStore>(this IServiceCollection services)
        where TStore : class, ITenantStore
    {
        services.AddSingleton<ITenantStore, TStore>();
        return services;
    }

    public static IServiceCollection AddCustomCurrentUserResolver<TResolver>(this IServiceCollection services)
        where TResolver : class, ICurrentUserResolver
    {
        services.AddScoped<ICurrentUserResolver, TResolver>();
        return services;
    }

    public static IServiceCollection AddCustomCurrentUserStore<TStore>(this IServiceCollection services)
        where TStore : class, ICurrentUserStore
    {
        services.AddScoped<ICurrentUserStore, TStore>();
        return services;
    }
}
