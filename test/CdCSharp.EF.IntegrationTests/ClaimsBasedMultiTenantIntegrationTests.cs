using CdCSharp.EF.IntegrationTests.Env;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CdCSharp.EF.IntegrationTests;

public class ClaimsBasedMultiTenantIntegrationTests : IClassFixture<ClaimsBasedMultiTenantWebApplicationFactory>
{
    private readonly ClaimsBasedMultiTenantWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ClaimsBasedMultiTenantIntegrationTests()
    {
        _factory = new ClaimsBasedMultiTenantWebApplicationFactory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    [Fact]
    public async Task GetProducts_WithClaimsTenant_ReturnsFilteredProducts()
    {
        // Arrange
        const string tenant1 = "claims-tenant-1";
        const string tenant2 = "claims-tenant-2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.AddRange(
                new TestProduct { Name = "Claims Product 1", Price = 100, Category = "Electronics", TenantId = tenant1 },
                new TestProduct { Name = "Claims Product 2", Price = 200, Category = "Books", TenantId = tenant1 }
            );
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new TestProduct { Name = "Claims Product 3", Price = 150, Category = "Electronics", TenantId = tenant2 });
            await context.SaveChangesAsync();
        });

        // Act - Make request with tenant claim
        _client.SetTestClaimsTenant(tenant1);
        HttpResponseMessage response = await _client.GetAsync("/api/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        TestProduct[]? products = JsonSerializer.Deserialize<TestProduct[]>(content, _jsonOptions);

        Assert.NotNull(products);
        Assert.Equal(2, products.Length);
        Assert.All(products, p => Assert.Equal(tenant1, p.TenantId));
        Assert.Contains(products, p => p.Name == "Claims Product 1");
        Assert.Contains(products, p => p.Name == "Claims Product 2");
    }

    [Fact]
    public async Task CreateProduct_WithClaimsTenant_AssignsTenantFromClaims()
    {
        // Arrange
        const string tenantId = "claims-tenant-create";
        CreateProductRequest request = new()
        {
            Name = "Claims Created Product",
            Price = 299.99m,
            Category = "Electronics"
        };

        // Act
        _client.SetTestClaimsTenant(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/products", request);

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
    public async Task GetProducts_WithoutClaimsTenant_ReturnsError()
    {
        // Arrange - Don't set any tenant claim
        _client.ClearTenantHeaders();

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _client.GetAsync("/api/products"));

        // Verificar el mensaje específico
        Assert.Equal("Current tenant ID is not set", exception.Message);
    }

    [Fact]
    public async Task GetProducts_WithDifferentClaimsTenants_ReturnsIsolatedData()
    {
        // Arrange
        const string tenant1 = "claims-tenant-isolation-1";
        const string tenant2 = "claims-tenant-isolation-2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.Add(new TestProduct { Name = "Isolated Product 1", Price = 100, Category = "Electronics", TenantId = tenant1 });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new TestProduct { Name = "Isolated Product 2", Price = 200, Category = "Books", TenantId = tenant2 });
            await context.SaveChangesAsync();
        });

        // Act - Get products for tenant1
        _client.SetTestClaimsTenant(tenant1);
        HttpResponseMessage tenant1Response = await _client.GetAsync("/api/products");
        string tenant1Content = await tenant1Response.Content.ReadAsStringAsync();
        TestProduct[]? tenant1Products = JsonSerializer.Deserialize<TestProduct[]>(tenant1Content, _jsonOptions);

        // Get products for tenant2
        _client.SetTestClaimsTenant(tenant2);
        HttpResponseMessage tenant2Response = await _client.GetAsync("/api/products");
        string tenant2Content = await tenant2Response.Content.ReadAsStringAsync();
        TestProduct[]? tenant2Products = JsonSerializer.Deserialize<TestProduct[]>(tenant2Content, _jsonOptions);

        // Assert
        Assert.NotNull(tenant1Products);
        Assert.NotNull(tenant2Products);

        Assert.Single(tenant1Products);
        Assert.Single(tenant2Products);

        Assert.Equal(tenant1, tenant1Products[0].TenantId);
        Assert.Equal(tenant2, tenant2Products[0].TenantId);

        Assert.Equal("Isolated Product 1", tenant1Products[0].Name);
        Assert.Equal("Isolated Product 2", tenant2Products[0].Name);
    }

    [Fact]
    public async Task UpdateProduct_WithClaimsTenant_UpdatesOnlyTenantProduct()
    {
        // Arrange
        const string tenant1 = "claims-tenant-update-1";
        const string tenant2 = "claims-tenant-update-2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.Add(new TestProduct { Id = 1, Name = "Claims Original 1", Price = 100, Category = "Electronics", TenantId = tenant1 });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new TestProduct { Id = 2, Name = "Claims Original 2", Price = 200, Category = "Books", TenantId = tenant2 });
            await context.SaveChangesAsync();
        });

        CreateProductRequest updateRequest = new()
        {
            Name = "Claims Updated Product",
            Price = 350.00m,
            Category = "Updated Category"
        };

        // Act
        _client.SetTestClaimsTenant(tenant1);
        HttpResponseMessage updateResponse = await _client.PutAsJsonAsync("/api/products/1", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Verify the update
        HttpResponseMessage getResponse = await _client.GetAsync("/api/products/1");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        string content = await getResponse.Content.ReadAsStringAsync();
        TestProduct? updatedProduct = JsonSerializer.Deserialize<TestProduct>(content, _jsonOptions);

        Assert.NotNull(updatedProduct);
        Assert.Equal("Claims Updated Product", updatedProduct.Name);
        Assert.Equal(350.00m, updatedProduct.Price);
        Assert.Equal(tenant1, updatedProduct.TenantId);
    }

    [Fact]
    public async Task CreateOrder_WithClaimsTenant_CreatesTenantSpecificOrder()
    {
        // Arrange
        const string tenantId = "claims-tenant-order";

        // First create some products
        await _factory.SeedDataAsync(tenantId, async context =>
        {
            context.Products.AddRange(
                new TestProduct { Id = 1, Name = "Claims Product 1", Price = 25.00m, Category = "Electronics", TenantId = tenantId },
                new TestProduct { Id = 2, Name = "Claims Product 2", Price = 35.00m, Category = "Books", TenantId = tenantId }
            );
            await context.SaveChangesAsync();
        });

        CreateOrderRequest orderRequest = new()
        {
            CustomerName = "Claims Customer",
            Items = new List<CreateOrderItemRequest>
        {
            new() { ProductId = 1, Quantity = 3 },
            new() { ProductId = 2, Quantity = 2 }
        }
        };

        // Act
        _client.SetTestClaimsTenant(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/orders", orderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        TestOrder? createdOrder = JsonSerializer.Deserialize<TestOrder>(content, _jsonOptions);

        Assert.NotNull(createdOrder);
        Assert.Equal(tenantId, createdOrder.TenantId);
        Assert.Equal("Claims Customer", createdOrder.CustomerName);
        Assert.Equal(2, createdOrder.Items.Count);
        Assert.Equal(145.00m, createdOrder.Total); // (3 * 25) + (2 * 35)

        // Verify all items have correct tenant
        Assert.All(createdOrder.Items, item => Assert.Equal(tenantId, item.TenantId));
    }

    [Fact]
    public async Task DeleteProduct_WithClaimsTenant_DeletesOnlyTenantProduct()
    {
        // Arrange
        const string tenant1 = "claims-tenant-delete-1";
        const string tenant2 = "claims-tenant-delete-2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.Add(new TestProduct { Id = 1, Name = "Claims Delete 1", Price = 100, Category = "Electronics", TenantId = tenant1 });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new TestProduct { Id = 2, Name = "Claims Delete 2", Price = 200, Category = "Books", TenantId = tenant2 });
            await context.SaveChangesAsync();
        });

        // Act
        _client.SetTestClaimsTenant(tenant1);
        HttpResponseMessage deleteResponse = await _client.DeleteAsync("/api/products/1");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deletion for tenant1
        HttpResponseMessage getResponse = await _client.GetAsync("/api/products/1");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        // Verify tenant2 product still exists
        _client.SetTestClaimsTenant(tenant2);
        HttpResponseMessage tenant2Response = await _client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, tenant2Response.StatusCode);

        string tenant2Content = await tenant2Response.Content.ReadAsStringAsync();
        TestProduct[]? tenant2Products = JsonSerializer.Deserialize<TestProduct[]>(tenant2Content, _jsonOptions);

        Assert.NotNull(tenant2Products);
        Assert.Single(tenant2Products);
        Assert.Equal("Claims Delete 2", tenant2Products[0].Name);
    }

    [Fact]
    public async Task GetOrders_WithClaimsTenant_ReturnsOnlyTenantOrders()
    {
        // Arrange
        const string tenant1 = "claims-tenant-orders-1";
        const string tenant2 = "claims-tenant-orders-2";

        // Create orders for both tenants
        await _factory.SeedDataAsync(tenant1, async context =>
        {
            TestProduct[] products = new[]
            {
            new TestProduct { Id = 1, Name = "T1 Product", Price = 10.00m, Category = "Electronics", TenantId = tenant1 }
        };
            context.Products.AddRange(products);

            TestOrder order = new()
            {
                CustomerName = "T1 Customer",
                OrderDate = DateTime.UtcNow,
                TenantId = tenant1,
                Items = new List<TestOrderItem>
            {
                new() { ProductId = 1, Product = products[0], Quantity = 1, UnitPrice = 10.00m, TenantId = tenant1 }
            },
                Total = 10.00m
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            TestProduct[] products = new[]
            {
            new TestProduct { Id = 2, Name = "T2 Product", Price = 20.00m, Category = "Books", TenantId = tenant2 }
        };
            context.Products.AddRange(products);

            TestOrder order = new()
            {
                CustomerName = "T2 Customer",
                OrderDate = DateTime.UtcNow,
                TenantId = tenant2,
                Items = new List<TestOrderItem>
            {
                new() { ProductId = 2, Product = products[0], Quantity = 1, UnitPrice = 20.00m, TenantId = tenant2 }
            },
                Total = 20.00m
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();
        });

        // Act - Get orders for tenant1
        _client.SetTestClaimsTenant(tenant1);
        HttpResponseMessage response = await _client.GetAsync("/api/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        TestOrder[]? orders = JsonSerializer.Deserialize<TestOrder[]>(content, _jsonOptions);

        Assert.NotNull(orders);
        Assert.Single(orders);
        Assert.Equal(tenant1, orders[0].TenantId);
        Assert.Equal("T1 Customer", orders[0].CustomerName);
        Assert.Equal(10.00m, orders[0].Total);
    }
}
