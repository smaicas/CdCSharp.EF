using CdCSharp.EF.Features.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CdCSharp.EF.Features.Identity;

public class IdentityFeatureProcessor : IFeatureProcessor
{
    private readonly IdentityFeature _feature;
    private readonly IServiceProvider _serviceProvider;

    public IdentityFeatureProcessor(IdentityFeature feature, IServiceProvider serviceProvider)
    {
        _feature = feature;
        _serviceProvider = serviceProvider;
    }

    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUserEntity(modelBuilder);
        ConfigureRoleEntity(modelBuilder);
        ConfigureUserClaimEntity(modelBuilder);
        ConfigureUserRoleEntity(modelBuilder);
        ConfigureUserLoginEntity(modelBuilder);
        ConfigureRoleClaimEntity(modelBuilder);
        ConfigureUserTokenEntity(modelBuilder);

    }

    public void OnModelCreatingEntity(ModelBuilder modelBuilder, Type entityType)
    {
        // Identity feature doesn't require process anything on model creating per each entity.
    }

    private void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        Type userType = _feature.UserType;

        modelBuilder.Entity(userType, b =>
        {
            b.ToTable(_feature.Configuration.UsersTableName);

            b.HasKey("Id");
            b.HasIndex("NormalizedUserName")
                .HasDatabaseName("UserNameIndex")
                .IsUnique();
            b.HasIndex("NormalizedEmail")
                .HasDatabaseName("EmailIndex");

            b.Property("ConcurrencyStamp").IsConcurrencyToken();
            b.Property("UserName").HasMaxLength(256);
            b.Property("NormalizedUserName").HasMaxLength(256);
            b.Property("Email").HasMaxLength(256);
            b.Property("NormalizedEmail").HasMaxLength(256);

            // Relations
            b.HasMany(_feature.UserClaimType)
                .WithOne()
                .HasForeignKey("UserId")
                .IsRequired();

            b.HasMany(_feature.UserLoginType)
                .WithOne()
                .HasForeignKey("UserId")
                .IsRequired();

            b.HasMany(_feature.UserTokenType)
                .WithOne()
                .HasForeignKey("UserId")
                .IsRequired();

            b.HasMany(_feature.UserRoleType)
                .WithOne()
                .HasForeignKey("UserId")
                .IsRequired();
        });
    }

    private void ConfigureRoleEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity(_feature.RoleType, b =>
        {
            b.ToTable(_feature.Configuration.RolesTableName);

            b.HasKey("Id");
            b.HasIndex("NormalizedName")
                .HasDatabaseName("RoleNameIndex")
                .IsUnique();

            b.Property("ConcurrencyStamp").IsConcurrencyToken();
            b.Property("Name").HasMaxLength(256);
            b.Property("NormalizedName").HasMaxLength(256);

            b.HasMany(_feature.UserRoleType)
                .WithOne()
                .HasForeignKey("RoleId")
                .IsRequired();

            b.HasMany(_feature.RoleClaimType)
                .WithOne()
                .HasForeignKey("RoleId")
                .IsRequired();
        });
    }

    private void ConfigureUserRoleEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity(_feature.UserRoleType, b =>
        {
            b.ToTable(_feature.Configuration.UserRolesTableName);
            b.HasKey(new[] { "UserId", "RoleId" });
        });
    }

    private void ConfigureUserClaimEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity(_feature.UserClaimType, b =>
        {
            b.ToTable(_feature.Configuration.UserClaimsTableName);
            b.HasKey("Id");
        });
    }

    private void ConfigureUserLoginEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity(_feature.UserLoginType, b =>
        {
            b.ToTable(_feature.Configuration.UserLoginsTableName);
            b.HasKey(new[] { "LoginProvider", "ProviderKey" });
        });
    }

    private void ConfigureRoleClaimEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity(_feature.RoleClaimType, b =>
        {
            b.ToTable(_feature.Configuration.RoleClaimsTableName);
            b.HasKey("Id");
        });
    }

    private void ConfigureUserTokenEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity(_feature.UserTokenType, b =>
        {
            b.ToTable(_feature.Configuration.UserTokensTableName);
            b.HasKey(new[] { "UserId", "LoginProvider", "Name" });
        });
    }

    public void OnSaveChanges(ChangeTracker changeTracker)
    {
        // Identity feature doesn't require process anything on save.
    }
}