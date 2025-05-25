using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.IntegrationTests.Int_ExtensibleDbContextWithIdentity;

public class ExtensibleDbContextWithIdentity_DbContext : ExtensibleDbContext
{
    public ExtensibleDbContextWithIdentity_DbContext(
        DbContextOptions<ExtensibleDbContextWithIdentity_DbContext> options,
        IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<ExtensibleDbContextWithIdentity_Product> Products { get; set; } = null!;

    // Identity DbSets are configured automatically by the IdentityFeatureProcessor
    public DbSet<IdentityUser<Guid>> Users { get; set; } = null!;
    public DbSet<IdentityRole<Guid>> Roles { get; set; } = null!;
}

public class ExtensibleDbContextWithIdentity_Product : IAuditableWithUserEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;

    // IAuditableWithUserEntity
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

public class ExtensibleDbContextWithIdentity_CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class ExtensibleDbContextWithIdentity_CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ExtensibleDbContextWithIdentity_CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
}
