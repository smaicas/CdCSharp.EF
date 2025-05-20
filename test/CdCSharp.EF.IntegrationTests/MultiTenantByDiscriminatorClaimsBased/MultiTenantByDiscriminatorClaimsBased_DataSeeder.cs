using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.IntegrationTests.MultiTenantByDiscriminatorClaimsBased;

public static class MultiTenantByDiscriminatorClaimsBased_DataSeeder
{
    public static async Task SeedBasicProductsAsync(MultiTenantByDiscriminatorClaimsBased_DbContext context, string tenantId)
    {
        MultiTenantByDiscriminatorClaimsBased_Product[] products = new[]
        {
        new MultiTenantByDiscriminatorClaimsBased_Product
        {
            Name = "Product 1",
            Price = 10.99m,
            Category = "Electronics",
            TenantId = tenantId
        },
        new MultiTenantByDiscriminatorClaimsBased_Product
        {
            Name = "Product 2",
            Price = 25.50m,
            Category = "Books",
            TenantId = tenantId
        }
    };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    public static async Task SeedOrderAsync(MultiTenantByDiscriminatorClaimsBased_DbContext context, string tenantId)
    {
        List<MultiTenantByDiscriminatorClaimsBased_Product> products = await context.Products
            .Where(p => p.TenantId == tenantId)
            .ToListAsync();

        if (!products.Any())
            await SeedBasicProductsAsync(context, tenantId);

        products = await context.Products
            .Where(p => p.TenantId == tenantId)
            .ToListAsync();

        MultiTenantByDiscriminatorClaimsBased_Order order = new()
        {
            CustomerName = "Test Customer",
            OrderDate = DateTime.UtcNow,
            TenantId = tenantId,
            Items = new List<MultiTenantByDiscriminatorClaimsBased_OrderItem>
        {
            new()
            {
                Product = products[0],
                ProductId = products[0].Id,
                Quantity = 2,
                UnitPrice = products[0].Price,
                TenantId = tenantId
            },
            new()
            {
                Product = products[1],
                ProductId = products[1].Id,
                Quantity = 1,
                UnitPrice = products[1].Price,
                TenantId = tenantId
            }
        }
        };

        order.Total = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }
}
