using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDiscriminatorCustomResolver;

public static class MultiTenantByDiscriminatorCustomResolver_DataSeeder
{
    public static async Task SeedBasicProductsAsync(MultiTenantByDiscriminatorCustomResolver_DbContext context)
    {
        MultiTenantByDiscriminatorCustomResolver_Product[] products = new[]
        {
        new MultiTenantByDiscriminatorCustomResolver_Product
        {
            Name = "Product 1",
            Price = 10.99m,
            Category = "Electronics"
        },
        new MultiTenantByDiscriminatorCustomResolver_Product
        {
            Name = "Product 2",
            Price = 25.50m,
            Category = "Books"
        }
    };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    public static async Task SeedOrderAsync(MultiTenantByDiscriminatorCustomResolver_DbContext context)
    {
        List<MultiTenantByDiscriminatorCustomResolver_Product> products = await context.Products.ToListAsync();
        if (!products.Any())
            await SeedBasicProductsAsync(context);

        products = await context.Products.ToListAsync();

        MultiTenantByDiscriminatorCustomResolver_Order order = new()
        {
            CustomerName = "Test Customer",
            OrderDate = DateTime.UtcNow,
            Items = new List<MultiTenantByDiscriminatorCustomResolver_OrderItem>
        {
            new()
            {
                Product = products[0],
                ProductId = products[0].Id,
                Quantity = 2,
                UnitPrice = products[0].Price
            },
            new()
            {
                Product = products[1],
                ProductId = products[1].Id,
                Quantity = 1,
                UnitPrice = products[1].Price
            }
        }
        };

        order.Total = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }
}
