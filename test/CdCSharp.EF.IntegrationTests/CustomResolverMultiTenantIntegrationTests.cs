using CdCSharp.EF.IntegrationTests.Env;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CdCSharp.EF.IntegrationTests;

public class CustomResolverMultiTenantIntegrationTests : IClassFixture<CustomResolverMultiTenantWebApplicationFactory>
{
    private readonly CustomResolverMultiTenantWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public CustomResolverMultiTenantIntegrationTests()
    {
        _factory = new CustomResolverMultiTenantWebApplicationFactory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    [Fact]
    public async Task GetProducts_WithTenantQueryString_ReturnsFilteredProducts()
    {
        // Arrange
        const string tenant1 = "query-tenant-1";
        const string tenant2 = "query-tenant-2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.AddRange(
                new TestProduct { Name = "Query Product 1", Price = 100, Category = "Electronics" },
                new TestProduct { Name = "Query Product 2", Price = 200, Category = "Books" }
            );
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new TestProduct { Name = "Query Product 3", Price = 150, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        // Act - Make request with tenant in query string
        string urlWithTenant = UrlHelper.WithTenantQuery("/api/products", tenant1);
        HttpResponseMessage response = await _client.GetAsync(urlWithTenant);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        TestProduct[]? products = JsonSerializer.Deserialize<TestProduct[]>(content, _jsonOptions);

        Assert.NotNull(products);
        Assert.Equal(2, products.Length);
        Assert.All(products, p => Assert.Equal(tenant1, p.TenantId));
        Assert.Contains(products, p => p.Name == "Query Product 1");
        Assert.Contains(products, p => p.Name == "Query Product 2");
    }

    [Fact]
    public async Task CreateProduct_WithTenantQueryString_AssignsTenantCorrectly()
    {
        // Arrange
        const string tenantId = "query-tenant-create";
        CreateProductRequest request = new()
        {
            Name = "Query Created Product",
            Price = 299.99m,
            Category = "Electronics"
        };

        // Act
        string urlWithTenant = UrlHelper.WithTenantQuery("/api/products", tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync(urlWithTenant, request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        TestProduct? createdProduct = JsonSerializer.Deserialize<TestProduct>(content, _jsonOptions);

        Assert.NotNull(createdProduct);
        Assert.Equal(tenantId, createdProduct.TenantId);
        Assert.Equal(request.Name, createdProduct.Name);
        Assert.Equal(request.Price, createdProduct.Price);
    }

    [Fact]
    public async Task GetProducts_WithoutTenantQueryString_ReturnsError()
    {
        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _client.GetAsync("/api/products"));

        // Verificar el mensaje específico
        Assert.Equal("Current tenant ID is not set", exception.Message);
    }

    [Fact]
    public async Task GetProducts_WithDifferentTenantQueryStrings_ReturnsIsolatedData()
    {
        // Arrange
        const string tenant1 = "query-tenant-isolation-1";
        const string tenant2 = "query-tenant-isolation-2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.Add(new TestProduct { Name = "Isolated Query Product 1", Price = 100, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new TestProduct { Name = "Isolated Query Product 2", Price = 200, Category = "Books" });
            await context.SaveChangesAsync();
        });

        // Act - Get products for tenant1
        string tenant1Url = UrlHelper.WithTenantQuery("/api/products", tenant1);
        HttpResponseMessage tenant1Response = await _client.GetAsync(tenant1Url);
        string tenant1Content = await tenant1Response.Content.ReadAsStringAsync();
        TestProduct[]? tenant1Products = JsonSerializer.Deserialize<TestProduct[]>(tenant1Content, _jsonOptions);

        // Get products for tenant2
        string tenant2Url = UrlHelper.WithTenantQuery("/api/products", tenant2);
        HttpResponseMessage tenant2Response = await _client.GetAsync(tenant2Url);
        string tenant2Content = await tenant2Response.Content.ReadAsStringAsync();
        TestProduct[]? tenant2Products = JsonSerializer.Deserialize<TestProduct[]>(tenant2Content, _jsonOptions);

        // Assert
        Assert.NotNull(tenant1Products);
        Assert.NotNull(tenant2Products);

        Assert.Single(tenant1Products);
        Assert.Single(tenant2Products);

        Assert.Equal(tenant1, tenant1Products[0].TenantId);
        Assert.Equal(tenant2, tenant2Products[0].TenantId);

        Assert.Equal("Isolated Query Product 1", tenant1Products[0].Name);
        Assert.Equal("Isolated Query Product 2", tenant2Products[0].Name);
    }

    [Fact]
    public async Task UpdateProduct_WithTenantQueryString_UpdatesOnlyTenantProduct()
    {
        // Arrange
        const string tenant1 = "query-tenant-update-1";
        const string tenant2 = "query-tenant-update-2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.Add(new TestProduct { Id = 1, Name = "Query Original 1", Price = 100, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new TestProduct { Id = 2, Name = "Query Original 2", Price = 200, Category = "Books" });
            await context.SaveChangesAsync();
        });

        CreateProductRequest updateRequest = new()
        {
            Name = "Query Updated Product",
            Price = 350.00m,
            Category = "Updated Category"
        };

        // Act
        string updateUrl = UrlHelper.WithTenantQuery("/api/products/1", tenant1);
        HttpResponseMessage updateResponse = await _client.PutAsJsonAsync(updateUrl, updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Verify the update
        string getUrl = UrlHelper.WithTenantQuery("/api/products/1", tenant1);
        HttpResponseMessage getResponse = await _client.GetAsync(getUrl);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        string content = await getResponse.Content.ReadAsStringAsync();
        TestProduct? updatedProduct = JsonSerializer.Deserialize<TestProduct>(content, _jsonOptions);

        Assert.NotNull(updatedProduct);
        Assert.Equal("Query Updated Product", updatedProduct.Name);
        Assert.Equal(350.00m, updatedProduct.Price);
        Assert.Equal(tenant1, updatedProduct.TenantId);
    }

    [Fact]
    public async Task CreateOrder_WithTenantQueryString_CreatesTenantSpecificOrder()
    {
        // Arrange
        const string tenantId = "query-tenant-order";

        // First create some products
        await _factory.SeedDataAsync(tenantId, async context =>
        {
            context.Products.AddRange(
                new TestProduct { Id = 1, Name = "Query Product 1", Price = 15.00m, Category = "Electronics" },
                new TestProduct { Id = 2, Name = "Query Product 2", Price = 25.00m, Category = "Books" }
            );
            await context.SaveChangesAsync();
        });

        CreateOrderRequest orderRequest = new()
        {
            CustomerName = "Query Customer",
            Items = new List<CreateOrderItemRequest>
        {
            new() { ProductId = 1, Quantity = 4 },
            new() { ProductId = 2, Quantity = 2 }
        }
        };

        // Act
        string orderUrl = UrlHelper.WithTenantQuery("/api/orders", tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync(orderUrl, orderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        TestOrder? createdOrder = JsonSerializer.Deserialize<TestOrder>(content, _jsonOptions);

        Assert.NotNull(createdOrder);
        Assert.Equal(tenantId, createdOrder.TenantId);
        Assert.Equal("Query Customer", createdOrder.CustomerName);
        Assert.Equal(2, createdOrder.Items.Count);
        Assert.Equal(110.00m, createdOrder.Total); // (4 * 15) + (2 * 25)

        // Verify all items have correct tenant
        Assert.All(createdOrder.Items, item => Assert.Equal(tenantId, item.TenantId));
    }

    [Fact]
    public async Task DeleteProduct_WithTenantQueryString_DeletesOnlyTenantProduct()
    {
        // Arrange
        const string tenant1 = "query-tenant-delete-1";
        const string tenant2 = "query-tenant-delete-2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.Add(new TestProduct { Id = 1, Name = "Query Delete 1", Price = 100, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new TestProduct { Id = 2, Name = "Query Delete 2", Price = 200, Category = "Books" });
            await context.SaveChangesAsync();
        });

        // Act
        string deleteUrl = UrlHelper.WithTenantQuery("/api/products/1", tenant1);
        HttpResponseMessage deleteResponse = await _client.DeleteAsync(deleteUrl);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deletion for tenant1
        string getUrl = UrlHelper.WithTenantQuery("/api/products/1", tenant1);
        HttpResponseMessage getResponse = await _client.GetAsync(getUrl);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        // Verify tenant2 product still exists
        string tenant2Url = UrlHelper.WithTenantQuery("/api/products", tenant2);
        HttpResponseMessage tenant2Response = await _client.GetAsync(tenant2Url);
        Assert.Equal(HttpStatusCode.OK, tenant2Response.StatusCode);

        string tenant2Content = await tenant2Response.Content.ReadAsStringAsync();
        TestProduct[]? tenant2Products = JsonSerializer.Deserialize<TestProduct[]>(tenant2Content, _jsonOptions);

        Assert.NotNull(tenant2Products);
        Assert.Single(tenant2Products);
        Assert.Equal("Query Delete 2", tenant2Products[0].Name);
    }

    [Fact]
    public async Task GetOrders_WithTenantQueryString_ReturnsOnlyTenantOrders()
    {
        // Arrange
        const string tenant1 = "query-tenant-orders-1";
        const string tenant2 = "query-tenant-orders-2";

        // Create orders for both tenants
        await _factory.SeedDataAsync(tenant1, async context =>
        {
            TestProduct[] products = new[]
            {
            new TestProduct { Id = 1, Name = "Q1 Product", Price = 30.00m, Category = "Electronics" }
        };
            context.Products.AddRange(products);

            TestOrder order = new()
            {
                CustomerName = "Q1 Customer",
                OrderDate = DateTime.UtcNow,
                Items = new List<TestOrderItem>
            {
                new() { ProductId = 1, Product = products[0], Quantity = 2, UnitPrice = 30.00m }
            },
                Total = 60.00m
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            TestProduct[] products = new[]
            {
            new TestProduct { Id = 2, Name = "Q2 Product", Price = 40.00m, Category = "Books" }
        };
            context.Products.AddRange(products);

            TestOrder order = new()
            {
                CustomerName = "Q2 Customer",
                OrderDate = DateTime.UtcNow,
                Items = new List<TestOrderItem>
            {
                new() { ProductId = 2, Product = products[0], Quantity = 1, UnitPrice = 40.00m }
            },
                Total = 40.00m
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();
        });

        // Act - Get orders for tenant1
        string ordersUrl = UrlHelper.WithTenantQuery("/api/orders", tenant1);
        HttpResponseMessage response = await _client.GetAsync(ordersUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        TestOrder[]? orders = JsonSerializer.Deserialize<TestOrder[]>(content, _jsonOptions);

        Assert.NotNull(orders);
        Assert.Single(orders);
        Assert.Equal(tenant1, orders[0].TenantId);
        Assert.Equal("Q1 Customer", orders[0].CustomerName);
        Assert.Equal(60.00m, orders[0].Total);
    }

    [Fact]
    public async Task GetProducts_WithMultipleQueryParameters_HandlesTenantCorrectly()
    {
        // Arrange
        const string tenantId = "query-tenant-multi-params";

        await _factory.SeedDataAsync(tenantId, async context =>
        {
            context.Products.Add(new TestProduct { Name = "Multi Param Product", Price = 100, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        // Act - Make request with multiple query parameters
        HttpResponseMessage response = await _client.GetAsync($"/api/products?category=Electronics&tenant={tenantId}&sort=name");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        TestProduct[]? products = JsonSerializer.Deserialize<TestProduct[]>(content, _jsonOptions);

        Assert.NotNull(products);
        Assert.Single(products);
        Assert.Equal(tenantId, products[0].TenantId);
        Assert.Equal("Multi Param Product", products[0].Name);
    }
}
