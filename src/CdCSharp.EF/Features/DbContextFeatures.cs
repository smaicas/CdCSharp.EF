﻿using CdCSharp.EF.Features.Auditing;
using CdCSharp.EF.Features.Identity;
using CdCSharp.EF.Features.MultiTenant;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.Features;

public class DbContextFeatures
{
    public AuditingFeature Auditing { get; set; } = new();
    public IdentityFeature Identity { get; set; } = new();
    public MultiTenantFeature MultiTenant { get; set; } = new();

    public static DbContextFeatures Default => new();
}

public class DbContextFeaturesBuilder
{
    private readonly DbContextFeatures _features = DbContextFeatures.Default;

    public DbContextFeaturesBuilder EnableAuditing(Action<AuditingConfiguration>? configureAuditing = null)
    {
        _features.Auditing.Enabled = true;

        configureAuditing?.Invoke(_features.Auditing.Configuration);

        return this;
    }

    public DbContextFeaturesBuilder EnableIdentity<TKey>(
    Action<IdentityConfiguration>? configureIdentity = null)
    where TKey : IEquatable<TKey> => EnableIdentity<TKey, IdentityUser<TKey>, IdentityRole<TKey>>(configureIdentity);

    public DbContextFeaturesBuilder EnableIdentity<TKey, TUser, TRole>(
        Action<IdentityConfiguration>? configureIdentity = null)
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
    {
        _features.Identity.Enabled = true;
        _features.Identity.UserType = typeof(TUser);
        _features.Identity.RoleType = typeof(TRole);
        _features.Identity.UserClaimType = typeof(IdentityUserClaim<TKey>);
        _features.Identity.UserRoleType = typeof(IdentityUserRole<TKey>);
        _features.Identity.UserLoginType = typeof(IdentityUserLogin<TKey>);
        _features.Identity.RoleClaimType = typeof(IdentityRoleClaim<TKey>);
        _features.Identity.UserTokenType = typeof(IdentityUserToken<TKey>);

        configureIdentity?.Invoke(_features.Identity.Configuration);

        return this;
    }

    public DbContextFeaturesBuilder EnableIdentity<TKey, TUser, TRole, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>(
    Action<IdentityConfiguration>? configureIdentity = null)
    where TKey : IEquatable<TKey>
    where TUser : IdentityUser<TKey>
    where TRole : IdentityRole<TKey>
    where TUserClaim : IdentityUserClaim<TKey>
    where TUserRole : IdentityUserRole<TKey>
    where TUserLogin : IdentityUserLogin<TKey>
    where TRoleClaim : IdentityRoleClaim<TKey>
    where TUserToken : IdentityUserToken<TKey>
    {
        _features.Identity.Enabled = true;
        _features.Identity.UserType = typeof(TUser);
        _features.Identity.RoleType = typeof(TRole);
        _features.Identity.UserClaimType = typeof(TUserClaim);
        _features.Identity.UserRoleType = typeof(TUserRole);
        _features.Identity.UserLoginType = typeof(TUserLogin);
        _features.Identity.RoleClaimType = typeof(TRoleClaim);
        _features.Identity.UserTokenType = typeof(TUserToken);

        configureIdentity?.Invoke(_features.Identity.Configuration);

        return this;
    }

    public DbContextFeaturesBuilder EnableMultiTenantByDiscriminator(Action<DbContextOptionsBuilder>? options = null)
    {
        _features.MultiTenant.Enabled = true;
        _features.MultiTenant.Configuration.Strategy = MultiTenantStrategy.Discriminator;

        if (options != null)
        {
            _features.MultiTenant.Configuration.DiscriminatorConfiguration = options;
        }

        return this;
    }
    public DbContextFeaturesBuilder EnableMultiTenantByDatabase(
        Action<MultiTenantByDatabaseBuilder> buildTenants)
    {
        MultiTenantByDatabaseBuilder builder = new();
        buildTenants(builder);
        Dictionary<string, Action<DbContextOptionsBuilder>> tenantConfigurations = builder.Build();

        if (!tenantConfigurations.Any())
            throw new ArgumentException("At least one tenant configuration is required.");

        _features.MultiTenant.Enabled = true;
        _features.MultiTenant.Configuration.Strategy = MultiTenantStrategy.Database;
        _features.MultiTenant.Configuration.DatabaseConfigurations = tenantConfigurations;

        return this;
    }

    public DbContextFeatures Build() => _features;
}
