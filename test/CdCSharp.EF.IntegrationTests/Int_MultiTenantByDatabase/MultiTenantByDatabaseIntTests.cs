using CdCSharp.EF.IntegrationTests._Common;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDatabase;

public class MultiTenantByDatabaseIntTests : IDisposable
{
    private readonly MultiTenantByDatabase_Factory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public MultiTenantByDatabaseIntTests()
    {
        _factory = new MultiTenantByDatabase_Factory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task GetProducts_WithMultipleDatabases_ShouldReturnIsolatedData()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        // Seed data for tenant1
        await _factory.SeedDataForTenantAsync(tenant1, async context =>
        {
            context.Products.AddRange(
                new MultiTenantByDatabase_Product { Name = "T1 Product 1", Price = 100, Category = "Electronics" },
                new MultiTenantByDatabase_Product { Name = "T1 Product 2", Price = 200, Category = "Books" }
            );
            await context.SaveChangesAsync();
        });

        // Seed data for tenant2
        await _factory.SeedDataForTenantAsync(tenant2, async context =>
        {
            context.Products.Add(new MultiTenantByDatabase_Product { Name = "T2 Product 1", Price = 150, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        // Act - Get products for tenant1
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage tenant1Response = await _client.GetAsync("/api/database/products");
        string tenant1Content = await tenant1Response.Content.ReadAsStringAsync();
        MultiTenantByDatabase_Product[]? tenant1Products = JsonSerializer.Deserialize<MultiTenantByDatabase_Product[]>(tenant1Content, _jsonOptions);

        // Get products for tenant2
        _client.SetTenantHeader(tenant2);
        HttpResponseMessage tenant2Response = await _client.GetAsync("/api/database/products");
        string tenant2Content = await tenant2Response.Content.ReadAsStringAsync();
        MultiTenantByDatabase_Product[]? tenant2Products = JsonSerializer.Deserialize<MultiTenantByDatabase_Product[]>(tenant2Content, _jsonOptions);

        // Assert
        Assert.NotNull(tenant1Products);
        Assert.NotNull(tenant2Products);
        Assert.Equal(2, tenant1Products.Length);
        Assert.Single(tenant2Products);

        // Verify names are correct (they use different databases)
        Assert.Contains(tenant1Products, p => p.Name == "T1 Product 1");
        Assert.Contains(tenant1Products, p => p.Name == "T1 Product 2");
        Assert.Contains(tenant2Products, p => p.Name == "T2 Product 1");

        // In database strategy, tenant IDs might not be set the same way
        // But data should still be isolated
        Assert.DoesNotContain(tenant1Products, p => p.Name.StartsWith("T2"));
        Assert.DoesNotContain(tenant2Products, p => p.Name.StartsWith("T1"));
    }

    [Fact]
    public async Task CreateProduct_InDifferentDatabases_ShouldNotInterfere()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        MultiTenantByDatabase_CreateProductRequest product1Request = new()
        {
            Name = "Database Product 1",
            Price = 100.00m,
            Category = "Electronics"
        };

        MultiTenantByDatabase_CreateProductRequest product2Request = new()
        {
            Name = "Database Product 2",
            Price = 200.00m,
            Category = "Books"
        };

        // Act - Create product for tenant1
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage response1 = await _client.PostAsJsonAsync("/api/database/products", product1Request);

        // Create product for tenant2
        _client.SetTenantHeader(tenant2);
        HttpResponseMessage response2 = await _client.PostAsJsonAsync("/api/database/products", product2Request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        // Verify isolation by checking each tenant only sees their products
        _client.SetTenantHeader(tenant1);
        MultiTenantByDatabase_Product[] tenant1Products = await GetProductsAsync();
        Assert.Contains(tenant1Products, p => p.Name == "Database Product 1");
        Assert.DoesNotContain(tenant1Products, p => p.Name == "Database Product 2");

        _client.SetTenantHeader(tenant2);
        MultiTenantByDatabase_Product[] tenant2Products = await GetProductsAsync();
        Assert.Contains(tenant2Products, p => p.Name == "Database Product 2");
        Assert.DoesNotContain(tenant2Products, p => p.Name == "Database Product 1");
    }

    [Fact]
    public async Task GetProducts_ForUnconfiguredTenant_ShouldReturnError()
    {
        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.GetAsync("/api/database/products"));

        // Verificar el mensaje específico
        Assert.Equal("Current tenant ID is not set", exception.Message);
    }

    [Fact]
    public async Task UpdateProduct_InSeparateDatabases_ShouldWorkIndependently()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        // Create initial products
        await _factory.SeedDataForTenantAsync(tenant1, async context =>
        {
            context.Products.Add(new MultiTenantByDatabase_Product { Id = 1, Name = "Original T1", Price = 100, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataForTenantAsync(tenant2, async context =>
        {
            context.Products.Add(new MultiTenantByDatabase_Product { Id = 1, Name = "Original T2", Price = 200, Category = "Books" });
            await context.SaveChangesAsync();
        });

        MultiTenantByDatabase_CreateProductRequest updateRequest = new()
        {
            Name = "Updated Product",
            Price = 300.00m,
            Category = "Updated"
        };

        // Act - Update product in tenant1 database
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage updateResponse = await _client.PutAsJsonAsync("/api/database/products/1", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Verify tenant1 product was updated
        _client.SetTenantHeader(tenant1);
        MultiTenantByDatabase_Product? tenant1Product = await GetProductByIdAsync(1);
        Assert.NotNull(tenant1Product);
        Assert.Equal("Updated Product", tenant1Product.Name);
        Assert.Equal(300.00m, tenant1Product.Price);

        // Verify tenant2 product was NOT affected
        _client.SetTenantHeader(tenant2);
        MultiTenantByDatabase_Product? tenant2Product = await GetProductByIdAsync(1);
        Assert.NotNull(tenant2Product);
        Assert.Equal("Original T2", tenant2Product.Name);
        Assert.Equal(200.00m, tenant2Product.Price);
    }

    [Fact]
    public async Task GetOrders_WithDifferentDatabases_ShouldReturnIsolatedData()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        // Create orders for both tenants, pero con un producto distinto para tenant2
        await _factory.SeedDataForTenantAsync(tenant1, async context =>
        {
            await MultiTenantByDatabase_DataSeeder.SeedBasicProductsAsync(context);
            await MultiTenantByDatabase_DataSeeder.SeedOrderAsync(context);
        });

        await _factory.SeedDataForTenantAsync(tenant2, async context =>
        {
            await MultiTenantByDatabase_DataSeeder.SeedBasicProductsAsync(context);

            // Modificamos Product 2 para que sea distinguible
            MultiTenantByDatabase_Product? product2 = await context.Products.FirstOrDefaultAsync(p => p.Name == "Product 2");
            if (product2 != null)
            {
                product2.Name = "Tenant2 Special Product";
                product2.Price = 99.99m;
                await context.SaveChangesAsync();
            }

            await MultiTenantByDatabase_DataSeeder.SeedOrderAsync(context);
        });

        // Act - Get orders for each tenant
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage tenant1Response = await _client.GetAsync("/api/database/orders");
        string tenant1Content = await tenant1Response.Content.ReadAsStringAsync();
        MultiTenantByDatabase_Order[]? tenant1Orders =
            JsonSerializer.Deserialize<MultiTenantByDatabase_Order[]>(tenant1Content, _jsonOptions);

        _client.SetTenantHeader(tenant2);
        HttpResponseMessage tenant2Response = await _client.GetAsync("/api/database/orders");
        string tenant2Content = await tenant2Response.Content.ReadAsStringAsync();
        MultiTenantByDatabase_Order[]? tenant2Orders =
            JsonSerializer.Deserialize<MultiTenantByDatabase_Order[]>(tenant2Content, _jsonOptions);

        // Assert
        Assert.NotNull(tenant1Orders);
        Assert.NotNull(tenant2Orders);
        Assert.Single(tenant1Orders);
        Assert.Single(tenant2Orders);

        // Obtenemos los nombres de productos de cada tenant para poder compararlos
        string[] tenant1ProductNames = tenant1Orders[0].Items.Select(i => i.Product.Name).OrderBy(n => n).ToArray();
        string[] tenant2ProductNames = tenant2Orders[0].Items.Select(i => i.Product.Name).OrderBy(n => n).ToArray();

        // Verificar que hay diferencias en los productos
        Assert.Contains("Product 2", tenant1ProductNames);
        Assert.DoesNotContain("Product 2", tenant2ProductNames);
        Assert.Contains("Tenant2 Special Product", tenant2ProductNames);
        Assert.DoesNotContain("Tenant2 Special Product", tenant1ProductNames);

        // Verificar aislamiento: cross-tenant access
        // Intentar acceder a un producto del tenant2 desde tenant1 con un ID específico
        _client.SetTenantHeader(tenant1);
        MultiTenantByDatabase_OrderItem productFromTenant2 = tenant2Orders[0].Items.First(i => i.Product.Name == "Tenant2 Special Product");
        HttpResponseMessage crossTenantResponse = await _client.GetAsync($"/api/database/products/{productFromTenant2.ProductId}");

        // Si encuentra un producto con el mismo ID, debe ser diferente al del tenant2
        if (crossTenantResponse.StatusCode == HttpStatusCode.OK)
        {
            string crossTenantContent = await crossTenantResponse.Content.ReadAsStringAsync();
            MultiTenantByDatabase_Product? crossTenantProduct = JsonSerializer.Deserialize<MultiTenantByDatabase_Product>(crossTenantContent, _jsonOptions);
            Assert.NotNull(crossTenantProduct);
            Assert.NotEqual("Tenant2 Special Product", crossTenantProduct.Name);
        }

        // Verificar que se puede acceder correctamente a los productos en su base de datos correcta
        _client.SetTenantHeader(tenant2);
        HttpResponseMessage tenant2ProductResponse = await _client.GetAsync($"/api/database/products/{productFromTenant2.ProductId}");
        Assert.Equal(HttpStatusCode.OK, tenant2ProductResponse.StatusCode);

        string tenant2ProductContent = await tenant2ProductResponse.Content.ReadAsStringAsync();
        MultiTenantByDatabase_Product? tenant2Product = JsonSerializer.Deserialize<MultiTenantByDatabase_Product>(tenant2ProductContent, _jsonOptions);
        Assert.NotNull(tenant2Product);
        Assert.Equal("Tenant2 Special Product", tenant2Product.Name);
    }

    private async Task<MultiTenantByDatabase_Product[]> GetProductsAsync()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/database/products");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MultiTenantByDatabase_Product[]>(content, _jsonOptions) ?? Array.Empty<MultiTenantByDatabase_Product>();
    }

    private async Task<MultiTenantByDatabase_Product?> GetProductByIdAsync(int id)
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/database/products/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MultiTenantByDatabase_Product>(content, _jsonOptions);
    }
}
