using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDiscriminatorWithIdentity;

public class MultiTenantByDiscriminatorWithIdentity_DbContext : MultiTenantDbContext
{
    public MultiTenantByDiscriminatorWithIdentity_DbContext(
        DbContextOptions<MultiTenantByDiscriminatorWithIdentity_DbContext> options,
        IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<MultiTenantByDiscriminatorWithIdentity_Product> Products { get; set; } = null!;
    public DbSet<MultiTenantByDiscriminatorWithIdentity_Order> Orders { get; set; } = null!;

    // Identity DbSets are configured automatically by the IdentityFeatureProcessor
    public DbSet<IdentityUser<Guid>> Users { get; set; } = null!;
    public DbSet<IdentityRole<Guid>> Roles { get; set; } = null!;
}

public class MultiTenantByDiscriminatorWithIdentity_Product : ITenantEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorWithIdentity_Order : ITenantEntity
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<MultiTenantByDiscriminatorWithIdentity_OrderItem> Items { get; set; } = new();
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorWithIdentity_OrderItem : ITenantEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    [JsonIgnore]
    public MultiTenantByDiscriminatorWithIdentity_Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public MultiTenantByDiscriminatorWithIdentity_Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorWithIdentity_CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorWithIdentity_CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public List<MultiTenantByDiscriminatorWithIdentity_CreateOrderItemRequest> Items { get; set; } = new();
}

public class MultiTenantByDiscriminatorWithIdentity_CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class MultiTenantByDiscriminatorWithIdentity_CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorWithIdentity_CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
}
