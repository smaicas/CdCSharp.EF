using CdCSharp.EF.Core;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.Configuration;

public class MultiTenantByDatabaseBuilder<TContext>
where TContext : MultiTenantDbContext
{
    private readonly Dictionary<string, Action<DbContextOptionsBuilder<TContext>>> _tenantConfigurations = new();

    public MultiTenantByDatabaseBuilder<TContext> AddTenant(string tenantId, Action<DbContextOptionsBuilder<TContext>> configureOptions)
    {
        _tenantConfigurations[tenantId] = configureOptions;
        return this;
    }

    internal IDictionary<string, Action<DbContextOptionsBuilder<TContext>>> Build() => _tenantConfigurations;
}
