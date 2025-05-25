using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDiscriminatorCustomResolver;

public class MultiTenantByDiscriminatorCustomResolverIntTests : IDisposable
{
    private readonly MultiTenantByDiscriminatorCustomResolver_Factory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public MultiTenantByDiscriminatorCustomResolverIntTests()
    {
        _factory = new MultiTenantByDiscriminatorCustomResolver_Factory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task GetProducts_WithTenantQueryString_ShouldReturnFilteredProducts()
    {
        // Arrange
        const string tenant1 = "query-tenant-1";
        const string tenant2 = "query-tenant-2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.AddRange(
                new MultiTenantByDiscriminatorCustomResolver_Product { Name = "Query Product 1", Price = 100, Category = "Electronics" },
                new MultiTenantByDiscriminatorCustomResolver_Product { Name = "Query Product 2", Price = 200, Category = "Books" }
            );
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminatorCustomResolver_Product { Name = "Query Product 3", Price = 150, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        // Act - Make request with tenant in query string
        string urlWithTenant = MultiTenantByDiscriminatorCustomResolver_UrlHelper.WithTenantQuery("/api/discriminator-resolver/products", tenant1);
        HttpResponseMessage response = await _client.GetAsync(urlWithTenant);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorCustomResolver_Product[]? products =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorCustomResolver_Product[]>(content, _jsonOptions);

        Assert.NotNull(products);
        Assert.Equal(2, products.Length);
        Assert.All(products, p => Assert.Equal(tenant1, p.TenantId));
        Assert.Contains(products, p => p.Name == "Query Product 1");
        Assert.Contains(products, p => p.Name == "Query Product 2");
    }

    [Fact]
    public async Task CreateProduct_WithTenantQueryString_ShouldAssignTenantCorrectly()
    {
        // Arrange
        const string tenantId = "query-tenant-create";
        MultiTenantByDiscriminatorCustomResolver_CreateProductRequest request = new()
        {
            Name = "Query Created Product",
            Price = 299.99m,
            Category = "Electronics"
        };

        // Act
        string urlWithTenant = MultiTenantByDiscriminatorCustomResolver_UrlHelper.WithTenantQuery("/api/discriminator-resolver/products", tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync(urlWithTenant, request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorCustomResolver_Product? createdProduct =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorCustomResolver_Product>(content, _jsonOptions);

        Assert.NotNull(createdProduct);
        Assert.Equal(tenantId, createdProduct.TenantId);
        Assert.Equal(request.Name, createdProduct.Name);
        Assert.Equal(request.Price, createdProduct.Price);
    }

    [Fact]
    public async Task GetProducts_WithoutTenantQueryString_ShouldReturnError()
    {
        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.GetAsync("/api/discriminator-resolver/products"));

        // Verificar el mensaje específico
        Assert.Equal("Current tenant ID is not set", exception.Message);
    }

    [Fact]
    public async Task GetProducts_WithDifferentTenantQueryStrings_ShouldReturnIsolatedData()
    {
        // Arrange
        const string tenant1 = "query-tenant-isolation-1";
        const string tenant2 = "query-tenant-isolation-2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminatorCustomResolver_Product { Name = "Isolated Query Product 1", Price = 100, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminatorCustomResolver_Product { Name = "Isolated Query Product 2", Price = 200, Category = "Books" });
            await context.SaveChangesAsync();
        });

        // Act - Get products for tenant1
        string tenant1Url = MultiTenantByDiscriminatorCustomResolver_UrlHelper.WithTenantQuery("/api/discriminator-resolver/products", tenant1);
        HttpResponseMessage tenant1Response = await _client.GetAsync(tenant1Url);
        string tenant1Content = await tenant1Response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorCustomResolver_Product[]? tenant1Products =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorCustomResolver_Product[]>(tenant1Content, _jsonOptions);

        // Get products for tenant2
        string tenant2Url = MultiTenantByDiscriminatorCustomResolver_UrlHelper.WithTenantQuery("/api/discriminator-resolver/products", tenant2);
        HttpResponseMessage tenant2Response = await _client.GetAsync(tenant2Url);
        string tenant2Content = await tenant2Response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorCustomResolver_Product[]? tenant2Products =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorCustomResolver_Product[]>(tenant2Content, _jsonOptions);

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
    public async Task UpdateProduct_WithTenantQueryString_ShouldUpdateOnlyTenantProduct()
    {
        // Arrange
        const string tenant1 = "query-tenant-update-1";
        const string tenant2 = "query-tenant-update-2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminatorCustomResolver_Product { Id = 1, Name = "Query Original 1", Price = 100, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminatorCustomResolver_Product { Id = 2, Name = "Query Original 2", Price = 200, Category = "Books" });
            await context.SaveChangesAsync();
        });

        MultiTenantByDiscriminatorCustomResolver_CreateProductRequest updateRequest = new()
        {
            Name = "Query Updated Product",
            Price = 350.00m,
            Category = "Updated Category"
        };

        // Act
        string updateUrl = MultiTenantByDiscriminatorCustomResolver_UrlHelper.WithTenantQuery("/api/discriminator-resolver/products/1", tenant1);
        HttpResponseMessage updateResponse = await _client.PutAsJsonAsync(updateUrl, updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Verify the update
        string getUrl = MultiTenantByDiscriminatorCustomResolver_UrlHelper.WithTenantQuery("/api/discriminator-resolver/products/1", tenant1);
        HttpResponseMessage getResponse = await _client.GetAsync(getUrl);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        string content = await getResponse.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorCustomResolver_Product? updatedProduct =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorCustomResolver_Product>(content, _jsonOptions);

        Assert.NotNull(updatedProduct);
        Assert.Equal("Query Updated Product", updatedProduct.Name);
        Assert.Equal(350.00m, updatedProduct.Price);
        Assert.Equal(tenant1, updatedProduct.TenantId);
    }

    [Fact]
    public async Task CreateOrder_WithTenantQueryString_ShouldCreateTenantSpecificOrder()
    {
        // Arrange
        const string tenantId = "query-tenant-order";

        // First create some products
        await _factory.SeedDataAsync(tenantId, async context =>
        {
            context.Products.AddRange(
                new MultiTenantByDiscriminatorCustomResolver_Product { Id = 1, Name = "Query Product 1", Price = 15.00m, Category = "Electronics" },
                new MultiTenantByDiscriminatorCustomResolver_Product { Id = 2, Name = "Query Product 2", Price = 25.00m, Category = "Books" }
            );
            await context.SaveChangesAsync();
        });

        MultiTenantByDiscriminatorCustomResolver_CreateOrderRequest orderRequest = new()
        {
            CustomerName = "Query Customer",
            Items = new List<MultiTenantByDiscriminatorCustomResolver_CreateOrderItemRequest>
        {
            new() { ProductId = 1, Quantity = 4 },
            new() { ProductId = 2, Quantity = 2 }
        }
        };

        // Act
        string orderUrl = MultiTenantByDiscriminatorCustomResolver_UrlHelper.WithTenantQuery("/api/discriminator-resolver/orders", tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync(orderUrl, orderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorCustomResolver_Order? createdOrder =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorCustomResolver_Order>(content, _jsonOptions);

        Assert.NotNull(createdOrder);
        Assert.Equal(tenantId, createdOrder.TenantId);
        Assert.Equal("Query Customer", createdOrder.CustomerName);
        Assert.Equal(2, createdOrder.Items.Count);
        Assert.Equal(110.00m, createdOrder.Total); // (4 * 15) + (2 * 25)

        // Verify all items have correct tenant
        Assert.All(createdOrder.Items, item => Assert.Equal(tenantId, item.TenantId));
    }

    [Fact]
    public async Task DeleteProduct_WithTenantQueryString_ShouldDeleteOnlyTenantProduct()
    {
        // Arrange
        const string tenant1 = "query-tenant-delete-1";
        const string tenant2 = "query-tenant-delete-2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminatorCustomResolver_Product { Id = 1, Name = "Query Delete 1", Price = 100, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminatorCustomResolver_Product { Id = 2, Name = "Query Delete 2", Price = 200, Category = "Books" });
            await context.SaveChangesAsync();
        });

        // Act
        string deleteUrl = MultiTenantByDiscriminatorCustomResolver_UrlHelper.WithTenantQuery("/api/discriminator-resolver/products/1", tenant1);
        HttpResponseMessage deleteResponse = await _client.DeleteAsync(deleteUrl);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deletion for tenant1
        string getUrl = MultiTenantByDiscriminatorCustomResolver_UrlHelper.WithTenantQuery("/api/discriminator-resolver/products/1", tenant1);
        HttpResponseMessage getResponse = await _client.GetAsync(getUrl);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        // Verify tenant2 product still exists
        string tenant2Url = MultiTenantByDiscriminatorCustomResolver_UrlHelper.WithTenantQuery("/api/discriminator-resolver/products", tenant2);
        HttpResponseMessage tenant2Response = await _client.GetAsync(tenant2Url);
        Assert.Equal(HttpStatusCode.OK, tenant2Response.StatusCode);

        string tenant2Content = await tenant2Response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorCustomResolver_Product[]? tenant2Products =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorCustomResolver_Product[]>(tenant2Content, _jsonOptions);

        Assert.NotNull(tenant2Products);
        Assert.Single(tenant2Products);
        Assert.Equal("Query Delete 2", tenant2Products[0].Name);
    }

    [Fact]
    public async Task GetOrders_WithTenantQueryString_ShouldReturnOnlyTenantOrders()
    {
        // Arrange
        const string tenant1 = "query-tenant-orders-1";
        const string tenant2 = "query-tenant-orders-2";

        // Create orders for both tenants
        await _factory.SeedDataAsync(tenant1, async context =>
        {
            MultiTenantByDiscriminatorCustomResolver_Product[] products = new[]
            {
            new MultiTenantByDiscriminatorCustomResolver_Product { Id = 1, Name = "Q1 Product", Price = 30.00m, Category = "Electronics" }
        };
            context.Products.AddRange(products);

            MultiTenantByDiscriminatorCustomResolver_Order order = new()
            {
                CustomerName = "Q1 Customer",
                OrderDate = DateTime.UtcNow,
                Items = new List<MultiTenantByDiscriminatorCustomResolver_OrderItem>
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
            MultiTenantByDiscriminatorCustomResolver_Product[] products = new[]
            {
            new MultiTenantByDiscriminatorCustomResolver_Product { Id = 2, Name = "Q2 Product", Price = 40.00m, Category = "Books" }
        };
            context.Products.AddRange(products);

            MultiTenantByDiscriminatorCustomResolver_Order order = new()
            {
                CustomerName = "Q2 Customer",
                OrderDate = DateTime.UtcNow,
                Items = new List<MultiTenantByDiscriminatorCustomResolver_OrderItem>
            {
                new() { ProductId = 2, Product = products[0], Quantity = 1, UnitPrice = 40.00m }
            },
                Total = 40.00m
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();
        });

        // Act - Get orders for tenant1
        string ordersUrl = MultiTenantByDiscriminatorCustomResolver_UrlHelper.WithTenantQuery("/api/discriminator-resolver/orders", tenant1);
        HttpResponseMessage response = await _client.GetAsync(ordersUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorCustomResolver_Order[]? orders =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorCustomResolver_Order[]>(content, _jsonOptions);

        Assert.NotNull(orders);
        Assert.Single(orders);
        Assert.Equal(tenant1, orders[0].TenantId);
        Assert.Equal("Q1 Customer", orders[0].CustomerName);
        Assert.Equal(60.00m, orders[0].Total);
    }

    [Fact]
    public async Task GetProducts_WithMultipleQueryParameters_ShouldHandleTenantCorrectly()
    {
        // Arrange
        const string tenantId = "query-tenant-multi-params";

        await _factory.SeedDataAsync(tenantId, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminatorCustomResolver_Product { Name = "Multi Param Product", Price = 100, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        // Act - Make request with multiple query parameters
        HttpResponseMessage response = await _client.GetAsync($"/api/discriminator-resolver/products?category=Electronics&tenant={tenantId}&sort=name");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorCustomResolver_Product[]? products =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorCustomResolver_Product[]>(content, _jsonOptions);

        Assert.NotNull(products);
        Assert.Single(products);
        Assert.Equal(tenantId, products[0].TenantId);
        Assert.Equal("Multi Param Product", products[0].Name);
    }
}
