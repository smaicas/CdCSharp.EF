namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDiscriminatorWithAuditing;

public static class MultiTenantByDiscriminatorWithAuditing_DataSeeder
{
    public static async Task SeedBasicProductsAsync(MultiTenantByDiscriminatorWithAuditing_DbContext context)
    {
        MultiTenantByDiscriminatorWithAuditing_Product[] products = new[]
        {
        new MultiTenantByDiscriminatorWithAuditing_Product
        {
            Name = "Product 1",
            Price = 10.99m,
            Category = "Electronics",
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow,
            CreatedBy = "SYSTEM",
            ModifiedBy = "SYSTEM"
        },
        new MultiTenantByDiscriminatorWithAuditing_Product
        {
            Name = "Product 2",
            Price = 25.50m,
            Category = "Books",
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow,
            CreatedBy = "SYSTEM",
            ModifiedBy = "SYSTEM"
        }
    };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}
