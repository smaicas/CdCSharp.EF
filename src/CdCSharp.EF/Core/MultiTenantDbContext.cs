using CdCSharp.EF.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.Core;

public abstract class MultiTenantDbContext : DbContext
{
    private string? _tenantId;
    private readonly ITenantStore _tenantStore;

    protected MultiTenantDbContext(DbContextOptions options, ITenantStore tenantStore)
        : base(options) => _tenantStore = tenantStore;

    protected MultiTenantDbContext(DbContextOptions options, IServiceProvider serviceProvider)
        : base(options) => _tenantStore = serviceProvider.GetRequiredService<ITenantStore>();

    public string? CurrentTenantId => _tenantId ?? _tenantStore.GetCurrentTenantId();

    internal void SetTenantId(string tenantId) => _tenantId = tenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply filters
        foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                System.Reflection.MethodInfo method = SetTenantFilterMethod.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private static readonly System.Reflection.MethodInfo SetTenantFilterMethod =
        typeof(MultiTenantDbContext).GetMethod(nameof(SetTenantFilter),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity => modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == CurrentTenantId);

    public override int SaveChanges()
    {
        SetTenantIdOnEntities();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantIdOnEntities();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetTenantIdOnEntities()
    {
        string? tenantId = CurrentTenantId;
        if (string.IsNullOrEmpty(tenantId))
            return;

        foreach (EntityEntry<ITenantEntity> entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.TenantId = tenantId;
            }
        }
    }
}
