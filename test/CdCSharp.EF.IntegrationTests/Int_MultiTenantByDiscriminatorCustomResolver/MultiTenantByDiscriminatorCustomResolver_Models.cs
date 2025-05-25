using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDiscriminatorCustomResolver;

public static class MultiTenantByDiscriminatorCustomResolver_UrlHelper
{
    public static string WithTenantQuery(string baseUrl, string tenantId)
    {
        string separator = baseUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}{separator}tenant={tenantId}";
    }
}

public class MultiTenantByDiscriminatorCustomResolver_Resolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MultiTenantByDiscriminatorCustomResolver_Resolver(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public Task<string?> ResolveTenantIdAsync()
    {
        HttpContext? context = _httpContextAccessor.HttpContext;
        if (context?.Request.Query.TryGetValue("tenant", out Microsoft.Extensions.Primitives.StringValues tenantValues) == true)
            return Task.FromResult(tenantValues.FirstOrDefault());

        return Task.FromResult<string?>(null);
    }
}

public class MultiTenantByDiscriminatorCustomResolver_DbContext : ExtensibleDbContext
{
    public MultiTenantByDiscriminatorCustomResolver_DbContext(
        DbContextOptions<MultiTenantByDiscriminatorCustomResolver_DbContext> options,
        IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<MultiTenantByDiscriminatorCustomResolver_Product> Products { get; set; } = null!;
    public DbSet<MultiTenantByDiscriminatorCustomResolver_Order> Orders { get; set; } = null!;
}

public class MultiTenantByDiscriminatorCustomResolver_Product : ITenantEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorCustomResolver_Order : ITenantEntity
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<MultiTenantByDiscriminatorCustomResolver_OrderItem> Items { get; set; } = new();
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorCustomResolver_OrderItem : ITenantEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    [JsonIgnore]
    public MultiTenantByDiscriminatorCustomResolver_Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public MultiTenantByDiscriminatorCustomResolver_Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string TenantId { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorCustomResolver_CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class MultiTenantByDiscriminatorCustomResolver_CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public List<MultiTenantByDiscriminatorCustomResolver_CreateOrderItemRequest> Items { get; set; } = new();
}

public class MultiTenantByDiscriminatorCustomResolver_CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
