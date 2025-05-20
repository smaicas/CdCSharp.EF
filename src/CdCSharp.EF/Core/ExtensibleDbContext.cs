using CdCSharp.EF.Features.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.Core;

public abstract class ExtensibleDbContext : DbContext
{
    private readonly IEnumerable<IFeatureProcessor> _processors;

    protected ExtensibleDbContext(
        DbContextOptions options,
        IServiceProvider serviceProvider) : base(options) => _processors = serviceProvider.GetService<IEnumerable<IFeatureProcessor>>() ?? new List<IFeatureProcessor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar las features
        foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IFeatureProcessor processor in _processors)
            {
                processor.OnModelCreating(modelBuilder, entityType.ClrType);
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