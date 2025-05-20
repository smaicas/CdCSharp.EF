using CdCSharp.EF.Configuration;
using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Core.Resolvers;
using CdCSharp.EF.Core.Stores;
using CdCSharp.EF.Features;
using CdCSharp.EF.Features.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.Extensions;

public static class ServiceCollectionExtensions
{
    // Método para contextos simples con features
    public static IServiceCollection AddExtensibleDbContext<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions,
        Func<DbContextFeaturesBuilder, DbContextFeaturesBuilder>? featuresBuilder = null)
        where TContext : ExtensibleDbContext
    {
        // Configurar features
        DbContextFeatures features = featuresBuilder?.Invoke(new DbContextFeaturesBuilder()).Build() ?? DbContextFeatures.Default;
        services.AddSingleton(features);

        // Registrar procesadores basándose en las features habilitadas
        RegisterFeatureProcessors(services, features);

        // Configurar current user services por defecto si no se han registrado previamente
        if (!services.Any(s => s.ServiceType == typeof(ICurrentUserStore)))
        {
            services.AddScoped<ICurrentUserStore, InMemoryCurrentUserStore>();
            services.AddScoped<ICurrentUserResolver, ClaimsCurrentUserResolver>();
        }

        // Configurar EF DbContext
        services.AddDbContext<TContext>(configureOptions);

        return services;
    }

    public static IServiceCollection AddMultiTenantByDiscriminatorDbContext<TContext>(
    this IServiceCollection services,
    Action<DbContextOptionsBuilder<TContext>> configureOptions,
    Func<DbContextFeaturesBuilder, DbContextFeaturesBuilder>? featuresBuilder = null)
    where TContext : MultiTenantDbContext
    {
        // Configurar features
        DbContextFeatures features = featuresBuilder?.Invoke(new DbContextFeaturesBuilder()).Build() ?? DbContextFeatures.Default;
        services.AddSingleton(features);

        // Registrar procesadores basándose en las features habilitadas
        RegisterFeatureProcessors(services, features);

        // Configurar current user services por defecto si no se han registrado previamente
        if (!services.Any(s => s.ServiceType == typeof(ICurrentUserStore)))
        {
            services.AddScoped<ICurrentUserStore, InMemoryCurrentUserStore>();
            services.AddScoped<ICurrentUserResolver, ClaimsCurrentUserResolver>();
        }

        // Configurar servicios multi-tenant
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

    // Método para contextos multi-tenant con features
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

        // Configurar features
        DbContextFeatures features = featuresBuilder?.Invoke(new DbContextFeaturesBuilder()).Build() ?? DbContextFeatures.Default;
        services.AddSingleton(features);

        // Registrar procesadores basándose en las features habilitadas
        RegisterFeatureProcessors(services, features);

        // Configurar servicios multi-tenant
        services.AddScoped<ITenantStore, InMemoryTenantStore>();
        services.AddScoped<ITenantResolver, HttpHeaderTenantResolver>();

        // Configurar current user services por defecto si no se han registrado previamente
        if (!services.Any(s => s.ServiceType == typeof(ICurrentUserStore)))
        {
            services.AddScoped<ICurrentUserStore, InMemoryCurrentUserStore>();
            services.AddScoped<ICurrentUserResolver, ClaimsCurrentUserResolver>();
        }

        MultiTenantConfiguration<TContext> configuration = new()
        {
            Strategy = MultiTenantStrategy.Database
        };

        // Añadir todas las configuraciones de tenants
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
        if (features.AuditingEnabled)
        {
            services.AddScoped<IFeatureProcessor, AuditingProcessor>(provider =>
                new AuditingProcessor(features.AuditingConfiguration, provider));
        }

        // Aquí se pueden agregar más procesadores en el futuro
        // if (features.SoftDeleteEnabled)
        // {
        //     services.AddScoped<IFeatureProcessor, SoftDeleteProcessor>(...);
        // }
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
