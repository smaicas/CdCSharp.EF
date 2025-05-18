using CdCSharp.EF.IntegrationTests.Env;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CdCSharp.EF.IntegrationTests;

public class DatabaseMultiTenantIntegrationTests : IDisposable
{
    private readonly DatabaseMultiTenantWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public DatabaseMultiTenantIntegrationTests()
    {
        _factory = new DatabaseMultiTenantWebApplicationFactory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task GetProducts_WithMultipleDatabases_ReturnsIsolatedData()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        // Seed data for tenant1
        await _factory.SeedDataForTenantAsync(tenant1, async context =>
        {
            context.Products.AddRange(
                new TestProduct { Name = "T1 Product 1", Price = 100, Category = "Electronics" },
                new TestProduct { Name = "T1 Product 2", Price = 200, Category = "Books" }
            );
            await context.SaveChangesAsync();
        });

        // Seed data for tenant2
        await _factory.SeedDataForTenantAsync(tenant2, async context =>
        {
            context.Products.Add(new TestProduct { Name = "T2 Product 1", Price = 150, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        // Act - Get products for tenant1
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage tenant1Response = await _client.GetAsync("/api/products");
        string tenant1Content = await tenant1Response.Content.ReadAsStringAsync();
        TestProduct[]? tenant1Products = JsonSerializer.Deserialize<TestProduct[]>(tenant1Content, _jsonOptions);

        // Get products for tenant2
        _client.SetTenantHeader(tenant2);
        HttpResponseMessage tenant2Response = await _client.GetAsync("/api/products");
        string tenant2Content = await tenant2Response.Content.ReadAsStringAsync();
        TestProduct[]? tenant2Products = JsonSerializer.Deserialize<TestProduct[]>(tenant2Content, _jsonOptions);

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
    public async Task CreateProduct_InDifferentDatabases_DoesNotInterfere()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        CreateProductRequest product1Request = new()
        {
            Name = "Database Product 1",
            Price = 100.00m,
            Category = "Electronics"
        };

        CreateProductRequest product2Request = new()
        {
            Name = "Database Product 2",
            Price = 200.00m,
            Category = "Books"
        };

        // Act - Create product for tenant1
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage response1 = await _client.PostAsJsonAsync("/api/products", product1Request);

        // Create product for tenant2
        _client.SetTenantHeader(tenant2);
        HttpResponseMessage response2 = await _client.PostAsJsonAsync("/api/products", product2Request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        // Verify isolation by checking each tenant only sees their products
        _client.SetTenantHeader(tenant1);
        TestProduct[] tenant1Products = await GetProductsAsync();
        Assert.Contains(tenant1Products, p => p.Name == "Database Product 1");
        Assert.DoesNotContain(tenant1Products, p => p.Name == "Database Product 2");

        _client.SetTenantHeader(tenant2);
        TestProduct[] tenant2Products = await GetProductsAsync();
        Assert.Contains(tenant2Products, p => p.Name == "Database Product 2");
        Assert.DoesNotContain(tenant2Products, p => p.Name == "Database Product 1");
    }

    [Fact]
    public async Task GetProducts_ForUnconfiguredTenant_ReturnsError()
    {
        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _client.GetAsync("/api/products"));

        // Verificar el mensaje específico
        Assert.Equal("Current tenant ID is not set", exception.Message);
    }

    [Fact]
    public async Task UpdateProduct_InSeparateDatabases_WorksIndependently()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        // Create initial products
        await _factory.SeedDataForTenantAsync(tenant1, async context =>
        {
            context.Products.Add(new TestProduct { Id = 1, Name = "Original T1", Price = 100, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataForTenantAsync(tenant2, async context =>
        {
            context.Products.Add(new TestProduct { Id = 1, Name = "Original T2", Price = 200, Category = "Books" });
            await context.SaveChangesAsync();
        });

        CreateProductRequest updateRequest = new()
        {
            Name = "Updated Product",
            Price = 300.00m,
            Category = "Updated"
        };

        // Act - Update product in tenant1 database
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage updateResponse = await _client.PutAsJsonAsync("/api/products/1", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Verify tenant1 product was updated
        _client.SetTenantHeader(tenant1);
        TestProduct? tenant1Product = await GetProductByIdAsync(1);
        Assert.NotNull(tenant1Product);
        Assert.Equal("Updated Product", tenant1Product.Name);
        Assert.Equal(300.00m, tenant1Product.Price);

        // Verify tenant2 product was NOT affected
        _client.SetTenantHeader(tenant2);
        TestProduct? tenant2Product = await GetProductByIdAsync(1);
        Assert.NotNull(tenant2Product);
        Assert.Equal("Original T2", tenant2Product.Name);
        Assert.Equal(200.00m, tenant2Product.Price);
    }

    [Fact]
    public async Task CreateOrder_WithItems_WorksInSeparateDatabases()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        // Create products for each tenant
        await _factory.SeedDataForTenantAsync(tenant1, async context =>
        {
            context.Products.AddRange(
                new TestProduct { Id = 1, Name = "T1 Product A", Price = 50.00m, Category = "Electronics" },
                new TestProduct { Id = 2, Name = "T1 Product B", Price = 30.00m, Category = "Books" }
            );
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataForTenantAsync(tenant2, async context =>
        {
            context.Products.AddRange(
                new TestProduct { Id = 1, Name = "T2 Product X", Price = 25.00m, Category = "Electronics" },
                new TestProduct { Id = 2, Name = "T2 Product Y", Price = 75.00m, Category = "Books" }
            );
            await context.SaveChangesAsync();
        });

        CreateOrderRequest orderRequest = new()
        {
            CustomerName = "Test Customer",
            Items = new List<CreateOrderItemRequest>
        {
            new() { ProductId = 1, Quantity = 2 },
            new() { ProductId = 2, Quantity = 1 }
        }
        };

        // Act - Create order for tenant1
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/orders", orderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        TestOrder? createdOrder = JsonSerializer.Deserialize<TestOrder>(content, _jsonOptions);

        Assert.NotNull(createdOrder);
        Assert.Equal("Test Customer", createdOrder.CustomerName);
        Assert.Equal(2, createdOrder.Items.Count);
        Assert.Equal(130.00m, createdOrder.Total); // (2 * 50) + (1 * 30)

        // Verify the products are the ones from tenant1's database
        Assert.Contains(createdOrder.Items, item => item.Product.Name == "T1 Product A");
        Assert.Contains(createdOrder.Items, item => item.Product.Name == "T1 Product B");
    }

    [Fact]
    public async Task DeleteProduct_InSeparateDatabases_OnlyAffectsCorrectDatabase()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        // Create products with same ID in both databases
        await _factory.SeedDataForTenantAsync(tenant1, async context =>
        {
            context.Products.Add(new TestProduct { Id = 1, Name = "T1 Product", Price = 100, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataForTenantAsync(tenant2, async context =>
        {
            context.Products.Add(new TestProduct { Id = 1, Name = "T2 Product", Price = 200, Category = "Books" });
            await context.SaveChangesAsync();
        });

        // Act - Delete product from tenant1 database
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage deleteResponse = await _client.DeleteAsync("/api/products/1");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify product was deleted from tenant1
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage tenant1GetResponse = await _client.GetAsync("/api/products/1");
        Assert.Equal(HttpStatusCode.NotFound, tenant1GetResponse.StatusCode);

        // Verify product still exists in tenant2
        _client.SetTenantHeader(tenant2);
        HttpResponseMessage tenant2GetResponse = await _client.GetAsync("/api/products/1");
        Assert.Equal(HttpStatusCode.OK, tenant2GetResponse.StatusCode);

        TestProduct? tenant2Product = await GetProductByIdAsync(1);
        Assert.NotNull(tenant2Product);
        Assert.Equal("T2 Product", tenant2Product.Name);
    }

    private async Task<TestProduct[]> GetProductsAsync()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TestProduct[]>(content, _jsonOptions) ?? Array.Empty<TestProduct>();
    }

    private async Task<TestProduct?> GetProductByIdAsync(int id)
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/products/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TestProduct>(content, _jsonOptions);
    }
}
