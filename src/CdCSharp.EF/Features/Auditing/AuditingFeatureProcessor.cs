using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Features.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CdCSharp.EF.Features.Auditing;

public class AuditingFeatureProcessor : IFeatureProcessor
{
    private readonly AuditingConfiguration _auditingConfiguration;
    private readonly IServiceProvider _serviceProvider;

    public AuditingFeatureProcessor(AuditingConfiguration auditingConfiguration, IServiceProvider serviceProvider)
    {
        _auditingConfiguration = auditingConfiguration;
        _serviceProvider = serviceProvider;
    }
    public void OnModelCreating(ModelBuilder modelBuilder) { }

    public void OnModelCreatingEntity(ModelBuilder modelBuilder, Type entityType, ExtensibleDbContext context)
    {
        if (typeof(IAuditableEntity).IsAssignableFrom(entityType))
        {
            MethodInfo method = typeof(AuditingFeatureProcessor)
                .GetMethod(nameof(ConfigureAuditing), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType);
            method.Invoke(this, new object[] { modelBuilder });
        }
    }

    private void ConfigureAuditing<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IAuditableEntity
    {
        modelBuilder.Entity<TEntity>(entity =>
        {
            entity.Property(e => e.CreatedDate)
                .HasDefaultValue(DateTime.UtcNow)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.LastModifiedDate)
                .HasDefaultValue(DateTime.UtcNow)
                .ValueGeneratedOnAddOrUpdate();
        });
    }

    public void OnSaveChanges(ChangeTracker changeTracker)
    {
        DateTime now = DateTime.UtcNow;
        string? currentUserId = GetCurrentUserId();

        foreach (EntityEntry<IAuditableEntity> entry in changeTracker.Entries<IAuditableEntity>())
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = now;
                    entry.Entity.LastModifiedDate = now;

                    if (entry.Entity is IAuditableWithUserEntity userAuditable)
                        HandleUserField(userAuditable, currentUserId, isCreation: true);
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedDate = now;

                    if (entry.Entity is IAuditableWithUserEntity userAuditable2)
                        HandleUserField(userAuditable2, currentUserId, isCreation: false);
                    break;
            }
    }

    private string? GetCurrentUserId()
    {
        ICurrentUserStore? userStore = _serviceProvider.GetService<ICurrentUserStore>();
        return userStore?.GetCurrentUserId();
    }

    private void HandleUserField(IAuditableWithUserEntity entity, string? currentUserId, bool isCreation)
    {
        if (!string.IsNullOrEmpty(currentUserId))
        {
            if (isCreation)
            {
                entity.CreatedBy = currentUserId;
                entity.ModifiedBy = currentUserId;
            }
            else
                entity.ModifiedBy = currentUserId;
            return;
        }

        // Handle behavior when no user is available
        switch (_auditingConfiguration.BehaviorWhenNoUser)
        {
            case AuditingBehavior.ThrowException:
                throw new InvalidOperationException("Current user ID is required when AuditingBehavior.ThrowException behavior is configured.");

            case AuditingBehavior.UseDefaultUser:
                if (isCreation)
                {
                    entity.CreatedBy = _auditingConfiguration.DefaultUserId;
                    entity.ModifiedBy = _auditingConfiguration.DefaultUserId;
                }
                else
                    entity.ModifiedBy = _auditingConfiguration.DefaultUserId;
                break;

            case AuditingBehavior.SaveAsNull:
                if (isCreation)
                {
                    entity.CreatedBy = null;
                    entity.ModifiedBy = null;
                }
                else
                    entity.ModifiedBy = null;
                break;

            case AuditingBehavior.SkipUserFields:
                break;
        }
    }

}
