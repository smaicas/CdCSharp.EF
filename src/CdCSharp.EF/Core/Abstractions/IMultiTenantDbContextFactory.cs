using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.Core.Abstractions;

public interface IMultiTenantDbContextFactory<TContext>
    where TContext : DbContext
{
    TContext CreateDbContext();
    TContext CreateDbContext(string tenantId);
}
