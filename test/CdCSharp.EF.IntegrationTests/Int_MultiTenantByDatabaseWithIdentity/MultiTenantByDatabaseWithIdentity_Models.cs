using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDatabaseWithIdentity;

public class MultiTenantByDatabaseWithIdentity_DbContext : MultiTenantDbContext
{
    public MultiTenantByDatabaseWithIdentity_DbContext(
        DbContextOptions<MultiTenantByDatabaseWithIdentity_DbContext> options,
        IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<MultiTenantByDatabaseWithIdentity_Product> Products { get; set; } = null!;
    public DbSet<IdentityUser<Guid>> Users { get; set; } = null!;
    public DbSet<IdentityRole<Guid>> Roles { get; set; } = null!;
}

public class MultiTenantByDatabaseWithIdentity_Product : ITenantEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDatabaseWithIdentity_CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class MultiTenantByDatabaseWithIdentity_CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class MultiTenantByDatabaseWithIdentity_CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
}
