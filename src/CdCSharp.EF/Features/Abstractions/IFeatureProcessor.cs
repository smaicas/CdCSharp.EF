using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CdCSharp.EF.Features.Abstractions;

public interface IFeatureProcessor
{
    void OnModelCreating(ModelBuilder modelBuilder, Type entityType);
    void OnSaveChanges(ChangeTracker changeTracker);
}
