using Microsoft.AspNetCore.Identity;

namespace CdCSharp.EF.Features.Identity;

public class IdentityFeature
{
    public bool Enabled { get; set; } = false;
    public IdentityConfiguration Configuration { get; set; } = new();
    public Type UserType { get; set; } = typeof(IdentityUser<Guid>);
    public Type RoleType { get; set; } = typeof(IdentityRole<Guid>);
    public Type UserClaimType { get; set; } = typeof(IdentityUserClaim<Guid>);
    public Type UserRoleType { get; set; } = typeof(IdentityUserRole<Guid>);
    public Type UserLoginType { get; set; } = typeof(IdentityUserLogin<Guid>);
    public Type RoleClaimType { get; set; } = typeof(IdentityRoleClaim<Guid>);
    public Type UserTokenType { get; set; } = typeof(IdentityUserToken<Guid>);
}

public class IdentityConfiguration
{
    public IdentityOptions Options { get; set; } = new();
    public string UsersTableName { get; set; } = "AspNetUsers";
    public string RolesTableName { get; set; } = "AspNetRoles";
    public string UserClaimsTableName { get; set; } = "AspNetUserClaims";
    public string UserRolesTableName { get; set; } = "AspNetUserRoles";
    public string UserLoginsTableName { get; set; } = "AspNetUserLogins";
    public string RoleClaimsTableName { get; set; } = "AspNetRoleClaims";
    public string UserTokensTableName { get; set; } = "AspNetUserTokens";
}
