using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDatabase;

public class MultiTenantByDatabase_DbContext : ExtensibleDbContext
{
    public MultiTenantByDatabase_DbContext(DbContextOptions<MultiTenantByDatabase_DbContext> options,
        IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<MultiTenantByDatabase_Product> Products { get; set; } = null!;
    public DbSet<MultiTenantByDatabase_Order> Orders { get; set; } = null!;
}

public class MultiTenantByDatabase_Product : ITenantEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDatabase_Order : ITenantEntity
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<MultiTenantByDatabase_OrderItem> Items { get; set; } = new();
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDatabase_OrderItem : ITenantEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    [JsonIgnore]
    public MultiTenantByDatabase_Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public MultiTenantByDatabase_Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDatabase_CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class MultiTenantByDatabase_CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public List<MultiTenantByDatabase_CreateOrderItemRequest> Items { get; set; } = new();
}

public class MultiTenantByDatabase_CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
