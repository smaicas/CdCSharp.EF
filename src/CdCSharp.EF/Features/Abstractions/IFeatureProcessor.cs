using CdCSharp.EF.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CdCSharp.EF.Features.Abstractions;

public interface IFeatureProcessor
{
    void OnModelCreating(ModelBuilder modelBuilder);
    void OnModelCreatingEntity(ModelBuilder modelBuilder, Type entityType, ExtensibleDbContext context);
    void OnSaveChanges(ChangeTracker changeTracker);
}
