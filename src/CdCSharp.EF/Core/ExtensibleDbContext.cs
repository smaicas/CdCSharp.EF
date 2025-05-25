using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Features.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.Core;

public abstract class ExtensibleDbContext : DbContext
{
    private readonly IEnumerable<IFeatureProcessor> _processors;
    private readonly ITenantStore? _tenantStore;
    private string? _tenantId;

    protected ExtensibleDbContext(
        DbContextOptions options,
        IServiceProvider serviceProvider) : base(options)
    {
        _processors = serviceProvider.GetService<IEnumerable<IFeatureProcessor>>() ?? new List<IFeatureProcessor>();
        _tenantStore = serviceProvider.GetService<ITenantStore>();
    }

    // _tenantId priorizes over service (TenantStore) value.
    // It is done to force tenant acces when CreateContexT(tenantId) ocurs.
    public string? CurrentTenantId => _tenantId ?? _tenantStore?.GetCurrentTenantId();

    internal void SetTenantId(string tenantId) => _tenantId = tenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (IFeatureProcessor processor in _processors)
        {
            processor.OnModelCreating(modelBuilder);
        }

        foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IFeatureProcessor processor in _processors)
            {
                processor.OnModelCreatingEntity(modelBuilder, entityType.ClrType, this);
            }
        }
    }

    public override int SaveChanges()
    {
        ApplyFeatures();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyFeatures();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyFeatures()
    {
        foreach (IFeatureProcessor processor in _processors)
        {
            processor.OnSaveChanges(ChangeTracker);
        }
    }
}