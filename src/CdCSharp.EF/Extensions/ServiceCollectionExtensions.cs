using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Core.Resolvers;
using CdCSharp.EF.Core.Stores;
using CdCSharp.EF.Features;
using CdCSharp.EF.Features.Abstractions;
using CdCSharp.EF.Features.Auditing;
using CdCSharp.EF.Features.Identity;
using CdCSharp.EF.Features.MultiTenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExtensibleDbContext<TContext>(
        this IServiceCollection services,
        Func<DbContextFeaturesBuilder, DbContextFeaturesBuilder>? featuresBuilder = null)
        where TContext : ExtensibleDbContext => services.AddExtensibleDbContext<TContext>(null, featuresBuilder);

    public static IServiceCollection AddExtensibleDbContext<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? configureOptions = null,
        Func<DbContextFeaturesBuilder, DbContextFeaturesBuilder>? featuresBuilder = null)
        where TContext : ExtensibleDbContext
    {
        // Configure features
        DbContextFeatures features = featuresBuilder?.Invoke(new DbContextFeaturesBuilder()).Build() ?? DbContextFeatures.Default;
        services.AddSingleton(features);

        // Register feature processors
        RegisterFeatureProcessors(services, features);

        // Configure required services
        ConfigureRequiredServices(services);

        // Configure multi-tenant services if enabled
        if (features.MultiTenant.Enabled)
        {
            ConfigureMultiTenantServices(services);
            ConfigureMultiTenantContext<TContext>(services, features.MultiTenant.Configuration, configureOptions);
        }
        else
        {
            if (configureOptions != null)
            {
                services.AddDbContext<TContext>(configureOptions);
            }
            else
            {
                throw new InvalidOperationException("DbContext configuration is required when multi-tenancy is not enabled");
            }
        }

        return services;
    }

    private static void ConfigureRequiredServices(IServiceCollection services)
    {
        if (!services.Any(s => s.ServiceType == typeof(ICurrentUserStore)))
        {
            services.AddScoped<ICurrentUserStore, InMemoryCurrentUserStore>();
            services.AddScoped<IWritableCurrentUserStore>(provider =>
                (IWritableCurrentUserStore)provider.GetRequiredService<ICurrentUserStore>());
            services.AddScoped<ICurrentUserResolver, ClaimsCurrentUserResolver>();
        }
    }

    private static void ConfigureMultiTenantServices(IServiceCollection services)
    {
        // By default uses InMemory store to store tenant.
        if (!services.Any(s => s.ServiceType == typeof(ITenantStore)))
        {
            services.AddScoped<ITenantStore, InMemoryTenantStore>();
        }

        // By default uses Http header to resolve tenant.
        if (!services.Any(s => s.ServiceType == typeof(ITenantResolver)))
        {
            services.AddScoped<ITenantResolver, HttpHeaderTenantResolver>();
        }
    }

    private static void ConfigureMultiTenantContext<TContext>(
        IServiceCollection services,
        MultiTenantConfiguration configuration,
        Action<DbContextOptionsBuilder>? fallbackOptions)
        where TContext : ExtensibleDbContext
    {
        // Resolve configuration
        ResolveMultiTenantConfiguration(configuration, fallbackOptions);

        // Register configuration
        services.AddSingleton(configuration);

        // Register factory
        services.AddScoped<IMultiTenantDbContextFactory<TContext>, MultiTenantDbContextFactory<TContext>>();

        // Uses factory to inyect TContext.
        services.AddScoped<TContext>(provider =>
        {
            IMultiTenantDbContextFactory<TContext> factory = provider.GetRequiredService<IMultiTenantDbContextFactory<TContext>>();
            return factory.CreateDbContext();
        });
    }

    private static void ResolveMultiTenantConfiguration(
        MultiTenantConfiguration configuration,
        Action<DbContextOptionsBuilder>? fallbackOptions)
    {
        if (configuration.Strategy == MultiTenantStrategy.Database)
        {
            if (configuration.DatabaseConfigurations == null || !configuration.DatabaseConfigurations.Any())
            {
                throw new InvalidOperationException("Database configurations are required for Database multi-tenancy strategy");
            }
        }
        else if (configuration.Strategy == MultiTenantStrategy.Discriminator)
        {
            // Priority: DiscriminatorConfiguration > fallbackOptions
            if (configuration.DiscriminatorConfiguration == null)
            {
                if (fallbackOptions != null)
                {
                    configuration.DiscriminatorConfiguration = fallbackOptions;
                }
                else
                {
                    throw new InvalidOperationException("Database configuration is required for Discriminator multi-tenancy strategy");
                }
            }
        }
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

        if (features.MultiTenant.Enabled)
        {
            services.AddScoped<IFeatureProcessor, MultiTenantFeatureProcessor>(provider =>
                new MultiTenantFeatureProcessor(features.MultiTenant.Configuration, provider));
        }
    }

    // Customization methods

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