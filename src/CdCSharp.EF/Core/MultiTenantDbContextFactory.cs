using CdCSharp.EF.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.Core;

public class MultiTenantDbContextFactory<TContext> : IMultiTenantDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITenantStore _tenantStore;
    private readonly MultiTenantConfiguration<TContext> _configuration;

    public MultiTenantDbContextFactory(
        IServiceProvider serviceProvider,
        ITenantStore tenantStore,
        MultiTenantConfiguration<TContext> configuration)
    {
        _serviceProvider = serviceProvider;
        _tenantStore = tenantStore;
        _configuration = configuration;
    }

    public TContext CreateDbContext()
    {
        string? tenantId = _tenantStore.GetCurrentTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new InvalidOperationException("Current tenant ID is not set");
        }

        return CreateDbContext(tenantId);
    }

    public TContext CreateDbContext(string tenantId)
    {
        if (_configuration.Strategy == MultiTenantStrategy.Database)
        {
            if (!_configuration.DatabaseConfigurations.TryGetValue(tenantId, out Action<DbContextOptionsBuilder<TContext>>? dbConfig))
            {
                throw new InvalidOperationException($"No database configuration found for tenant: {tenantId}");
            }

            DbContextOptionsBuilder<TContext> options = new();
            dbConfig(options);

            return (TContext)Activator.CreateInstance(typeof(TContext), options.Options, _serviceProvider)!;
        }
        else
        {
            DbContextOptionsBuilder<TContext> options = new();
            _configuration.DiscriminatorConfiguration?.Invoke(options);

            TContext context = (TContext)Activator.CreateInstance(typeof(TContext), options.Options, _serviceProvider)!;

            if (context is MultiTenantDbContext multiTenantContext)
            {
                multiTenantContext.SetTenantId(tenantId);
            }

            return context;
        }
    }
}
