using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDiscriminatorWithAuditing;

public class MultiTenantByDiscriminatorWithAuditing_DbContext : MultiTenantDbContext
{
    public MultiTenantByDiscriminatorWithAuditing_DbContext(
        DbContextOptions<MultiTenantByDiscriminatorWithAuditing_DbContext> options,
        IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<MultiTenantByDiscriminatorWithAuditing_Product> Products { get; set; } = null!;
    public DbSet<MultiTenantByDiscriminatorWithAuditing_Order> Orders { get; set; } = null!;
}

public class MultiTenantByDiscriminatorWithAuditing_Product : ITenantEntity, IAuditableWithUserEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;

    // IAuditableWithUserEntity
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

public class MultiTenantByDiscriminatorWithAuditing_Order : ITenantEntity, IAuditableEntity
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<MultiTenantByDiscriminatorWithAuditing_OrderItem> Items { get; set; } = new();
    public string TenantId { get; set; } = string.Empty;

    // IAuditableEntity
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
}

public class MultiTenantByDiscriminatorWithAuditing_OrderItem : ITenantEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    [JsonIgnore]
    public MultiTenantByDiscriminatorWithAuditing_Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public MultiTenantByDiscriminatorWithAuditing_Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorWithAuditing_CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorWithAuditing_CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public List<MultiTenantByDiscriminatorWithAuditing_CreateOrderItemRequest> Items { get; set; } = new();
}

public class MultiTenantByDiscriminatorWithAuditing_CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
