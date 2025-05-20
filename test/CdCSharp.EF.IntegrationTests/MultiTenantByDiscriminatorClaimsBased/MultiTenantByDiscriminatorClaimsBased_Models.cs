using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace CdCSharp.EF.IntegrationTests.MultiTenantByDiscriminatorClaimsBased;

public class MultiTenantByDiscriminatorClaimsBased_DbContext : MultiTenantDbContext
{
    public MultiTenantByDiscriminatorClaimsBased_DbContext(
        DbContextOptions<MultiTenantByDiscriminatorClaimsBased_DbContext> options,
        IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<MultiTenantByDiscriminatorClaimsBased_Product> Products { get; set; } = null!;
    public DbSet<MultiTenantByDiscriminatorClaimsBased_Order> Orders { get; set; } = null!;
}

public class MultiTenantByDiscriminatorClaimsBased_Product : ITenantEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorClaimsBased_Order : ITenantEntity
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<MultiTenantByDiscriminatorClaimsBased_OrderItem> Items { get; set; } = new();
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorClaimsBased_OrderItem : ITenantEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    [JsonIgnore]
    public MultiTenantByDiscriminatorClaimsBased_Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public MultiTenantByDiscriminatorClaimsBased_Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorClaimsBased_CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorClaimsBased_CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public List<MultiTenantByDiscriminatorClaimsBased_CreateOrderItemRequest> Items { get; set; } = new();
}

public class MultiTenantByDiscriminatorClaimsBased_CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
