using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Core.Resolvers;
using CdCSharp.EF.Core.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMultiTenantByDiscriminatorDbContext<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder<TContext>> configureOptions)
        where TContext : DbContext
    {
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

    public static IServiceCollection AddMultiTenantByDatabaseDbContext<TContext>(
        this IServiceCollection services,
        string tenantId,
        Action<DbContextOptionsBuilder<TContext>> configureOptions)
        where TContext : DbContext
    {
        // Configure base services only in first call
        if (!services.Any(s => s.ServiceType == typeof(ITenantStore)))
        {
            services.AddScoped<ITenantStore, InMemoryTenantStore>();
            services.AddScoped<ITenantResolver, HttpHeaderTenantResolver>();

            MultiTenantConfiguration<TContext> configuration = new()
            {
                Strategy = MultiTenantStrategy.Database
            };

            services.AddSingleton(configuration);
            services.AddScoped<IMultiTenantDbContextFactory<TContext>, MultiTenantDbContextFactory<TContext>>();
            services.AddScoped<TContext>(provider =>
            {
                IMultiTenantDbContextFactory<TContext> factory = provider.GetRequiredService<IMultiTenantDbContextFactory<TContext>>();
                return factory.CreateDbContext();
            });
        }

        ServiceDescriptor? existingConfig = services.FirstOrDefault(s => s.ServiceType == typeof(MultiTenantConfiguration<TContext>));
        if (existingConfig?.ImplementationInstance is MultiTenantConfiguration<TContext> config)
        {
            config.DatabaseConfigurations[tenantId] = configureOptions;
        }

        return services;
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
}
