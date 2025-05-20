using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace CdCSharp.EF.IntegrationTests.MultiTenantByDiscriminator;

public class MultiTenantByDiscriminator_DbContext : MultiTenantDbContext
{
    public MultiTenantByDiscriminator_DbContext(
        DbContextOptions<MultiTenantByDiscriminator_DbContext> options,
        IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<MultiTenantByDiscriminator_Product> Products { get; set; } = null!;
    public DbSet<MultiTenantByDiscriminator_Order> Orders { get; set; } = null!;
}

public class MultiTenantByDiscriminator_Product : ITenantEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminator_Order : ITenantEntity
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<MultiTenantByDiscriminator_OrderItem> Items { get; set; } = new();
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminator_OrderItem : ITenantEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    [JsonIgnore]
    public MultiTenantByDiscriminator_Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public MultiTenantByDiscriminator_Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminator_CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminator_CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public List<MultiTenantByDiscriminator_CreateOrderItemRequest> Items { get; set; } = new();
}

public class MultiTenantByDiscriminator_CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
