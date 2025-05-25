using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Features.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CdCSharp.EF.Features.MultiTenant;

public class MultiTenantFeatureProcessor : IFeatureProcessor
{
    private readonly MultiTenantConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public MultiTenantFeatureProcessor(MultiTenantConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        // NOOP
    }

    public void OnModelCreatingEntity(ModelBuilder modelBuilder, Type entityType, ExtensibleDbContext context)
    {
        if (_configuration.Strategy == MultiTenantStrategy.Discriminator &&
            typeof(ITenantEntity).IsAssignableFrom(entityType))
        {
            ApplyTenantFilter(modelBuilder, entityType, context);
        }
    }

    public void OnSaveChanges(ChangeTracker changeTracker) =>
        // It sets TenantId also for Database strategy.
        // TODO: Parameter to decide behavior for DataBase strategy. 
        // Since TenantId will be always present for ITenantEntity
        SetTenantIdOnEntities(changeTracker);

    private void ApplyTenantFilter(ModelBuilder modelBuilder, Type entityType, ExtensibleDbContext context)
    {
        MethodInfo method = typeof(MultiTenantFeatureProcessor)
            .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(entityType);

        method.Invoke(this, new object[] { modelBuilder, context });
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder, ExtensibleDbContext context)
        where TEntity : class, ITenantEntity => modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == context.CurrentTenantId);

    private void SetTenantIdOnEntities(ChangeTracker changeTracker)
    {
        ITenantStore tenantStore = _serviceProvider.GetRequiredService<ITenantStore>();
        string? tenantId = tenantStore.GetCurrentTenantId();

        if (string.IsNullOrEmpty(tenantId))
            return;

        foreach (EntityEntry<ITenantEntity> entry in changeTracker.Entries<ITenantEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.TenantId = tenantId;
            }
        }
    }
}