using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDiscriminatorWithIdentity;

public static class MultiTenantByDiscriminatorWithIdentity_DataSeeder
{
    public static async Task SeedBasicProductsAsync(MultiTenantByDiscriminatorWithIdentity_DbContext context)
    {
        MultiTenantByDiscriminatorWithIdentity_Product[] products = new[]
        {
        new MultiTenantByDiscriminatorWithIdentity_Product
        {
            Name = "Product 1",
            Price = 10.99m,
            Category = "Electronics"
        },
        new MultiTenantByDiscriminatorWithIdentity_Product
        {
            Name = "Product 2",
            Price = 25.50m,
            Category = "Books"
        }
    };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    public static async Task SeedBasicUsersAsync(MultiTenantByDiscriminatorWithIdentity_DbContext context)
    {
        IdentityUser<Guid>[] users = new[]
        {
        new IdentityUser<Guid>
        {
            Id = Guid.NewGuid(),
            UserName = "user1",
            Email = "user1@test.com",
            NormalizedUserName = "USER1",
            NormalizedEmail = "USER1@TEST.COM",
            EmailConfirmed = true
        },
        new IdentityUser<Guid>
        {
            Id = Guid.NewGuid(),
            UserName = "user2",
            Email = "user2@test.com",
            NormalizedUserName = "USER2",
            NormalizedEmail = "USER2@TEST.COM",
            EmailConfirmed = true
        }
    };

        context.Users.AddRange(users);
        await context.SaveChangesAsync();
    }

    public static async Task SeedBasicRolesAsync(MultiTenantByDiscriminatorWithIdentity_DbContext context)
    {
        IdentityRole<Guid>[] roles = new[]
        {
        new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            NormalizedName = "ADMIN"
        },
        new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = "User",
            NormalizedName = "USER"
        }
    };

        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();
    }

    public static async Task SeedOrderAsync(MultiTenantByDiscriminatorWithIdentity_DbContext context)
    {
        List<MultiTenantByDiscriminatorWithIdentity_Product> products = await context.Products.ToListAsync();
        if (!products.Any())
            await SeedBasicProductsAsync(context);

        products = await context.Products.ToListAsync();

        MultiTenantByDiscriminatorWithIdentity_Order order = new()
        {
            CustomerName = "Test Customer",
            OrderDate = DateTime.UtcNow,
            Items = new List<MultiTenantByDiscriminatorWithIdentity_OrderItem>
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
