using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.Features.MultiTenant;

public class MultiTenantByDatabaseBuilder
{
    private readonly Dictionary<string, Action<DbContextOptionsBuilder>> _tenantConfigurations = new();

    public MultiTenantByDatabaseBuilder AddTenant(string tenantId, Action<DbContextOptionsBuilder> configureOptions)
    {
        _tenantConfigurations[tenantId] = configureOptions;
        return this;
    }

    internal Dictionary<string, Action<DbContextOptionsBuilder>> Build() => _tenantConfigurations;
}
