using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.IntegrationTests.MultiTenantByDiscriminator;

public static class MultiTenantByDiscriminator_DataSeeder
{
    public static async Task SeedBasicProductsAsync(MultiTenantByDiscriminator_DbContext context)
    {
        MultiTenantByDiscriminator_Product[] products = new[]
        {
        new MultiTenantByDiscriminator_Product { Name = "Product 1", Price = 10.99m, Category = "Electronics" },
        new MultiTenantByDiscriminator_Product { Name = "Product 2", Price = 25.50m, Category = "Books" },
        new MultiTenantByDiscriminator_Product { Name = "Product 3", Price = 15.75m, Category = "Electronics" }
    };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    public static async Task SeedOrdersAsync(MultiTenantByDiscriminator_DbContext context)
    {
        List<MultiTenantByDiscriminator_Product> products = await context.Products.ToListAsync();
        if (!products.Any())
            await SeedBasicProductsAsync(context);

        products = await context.Products.ToListAsync();

        MultiTenantByDiscriminator_Order order = new()
        {
            CustomerName = "Test Customer",
            OrderDate = DateTime.UtcNow,
            Items = new List<MultiTenantByDiscriminator_OrderItem>
        {
            new() { Product = products[0], ProductId = products[0].Id, Quantity = 2, UnitPrice = products[0].Price },
            new() { Product = products[1], ProductId = products[1].Id, Quantity = 1, UnitPrice = products[1].Price }
        }
        };

        order.Total = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }
}
