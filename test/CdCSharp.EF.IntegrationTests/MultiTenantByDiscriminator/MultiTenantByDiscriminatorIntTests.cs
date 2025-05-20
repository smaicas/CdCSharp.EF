using CdCSharp.EF.IntegrationTests._Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CdCSharp.EF.IntegrationTests.MultiTenantByDiscriminator;

public class MultiTenantByDiscriminatorIntTests : IDisposable
{
    private readonly MultiTenantByDiscriminator_Factory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public MultiTenantByDiscriminatorIntTests()
    {
        _factory = new MultiTenantByDiscriminator_Factory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task GetProducts_WithTenantHeader_ShouldReturnOnlyTenantProducts()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminator_Product { Name = "Tenant1 Product 1", Price = 100, Category = "Electronics" });
            context.Products.Add(new MultiTenantByDiscriminator_Product { Name = "Tenant1 Product 2", Price = 200, Category = "Books" });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminator_Product { Name = "Tenant2 Product 1", Price = 150, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        // Act - Get products for tenant1
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage tenant1Response = await _client.GetAsync("/api/discriminator/products");
        string tenant1Content = await tenant1Response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminator_Product[]? tenant1Products = JsonSerializer.Deserialize<MultiTenantByDiscriminator_Product[]>(tenant1Content, _jsonOptions);

        // Act - Get products for tenant2
        _client.SetTenantHeader(tenant2);
        HttpResponseMessage tenant2Response = await _client.GetAsync("/api/discriminator/products");
        string tenant2Content = await tenant2Response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminator_Product[]? tenant2Products = JsonSerializer.Deserialize<MultiTenantByDiscriminator_Product[]>(tenant2Content, _jsonOptions);

        // Assert
        Assert.NotNull(tenant1Products);
        Assert.NotNull(tenant2Products);
        Assert.Equal(2, tenant1Products.Length);
        Assert.Single(tenant2Products);

        Assert.All(tenant1Products, p => Assert.Equal(tenant1, p.TenantId));
        Assert.All(tenant2Products, p => Assert.Equal(tenant2, p.TenantId));

        Assert.Contains(tenant1Products, p => p.Name == "Tenant1 Product 1");
        Assert.Contains(tenant1Products, p => p.Name == "Tenant1 Product 2");
        Assert.Contains(tenant2Products, p => p.Name == "Tenant2 Product 1");
    }

    [Fact]
    public async Task CreateProduct_WithTenantHeader_ShouldAssignTenantIdAutomatically()
    {
        // Arrange
        const string tenantId = "tenant1";
        MultiTenantByDiscriminator_CreateProductRequest request = new()
        {
            Name = "New Product",
            Price = 299.99m,
            Category = "Electronics"
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/discriminator/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminator_Product? createdProduct = JsonSerializer.Deserialize<MultiTenantByDiscriminator_Product>(content, _jsonOptions);

        Assert.NotNull(createdProduct);
        Assert.Equal(tenantId, createdProduct.TenantId);
        Assert.Equal(request.Name, createdProduct.Name);
        Assert.Equal(request.Price, createdProduct.Price);
        Assert.Equal(request.Category, createdProduct.Category);
    }

    [Fact]
    public async Task GetProducts_WithoutTenantHeader_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _client.ClearTenantHeaders();

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _client.GetAsync("/api/discriminator/products"));

        // Verificar el mensaje específico
        Assert.Equal("Current tenant ID is not set", exception.Message);
    }

    [Fact]
    public async Task UpdateProduct_WithTenantHeader_ShouldUpdateOnlyTenantProduct()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        // Create products for both tenants
        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminator_Product { Id = 1, Name = "Tenant1 Product", Price = 100, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminator_Product { Id = 2, Name = "Tenant2 Product", Price = 200, Category = "Books" });
            await context.SaveChangesAsync();
        });

        MultiTenantByDiscriminator_CreateProductRequest updateRequest = new()
        {
            Name = "Updated Product",
            Price = 350.00m,
            Category = "Updated Category"
        };

        // Act - Try to update product with tenant1 header (should only update tenant1's product)
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage updateResponse = await _client.PutAsJsonAsync("/api/discriminator/products/1", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Verify the update
        HttpResponseMessage getResponse = await _client.GetAsync("/api/discriminator/products/1");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        string content = await getResponse.Content.ReadAsStringAsync();
        MultiTenantByDiscriminator_Product? updatedProduct = JsonSerializer.Deserialize<MultiTenantByDiscriminator_Product>(content, _jsonOptions);

        Assert.NotNull(updatedProduct);
        Assert.Equal("Updated Product", updatedProduct.Name);
        Assert.Equal(350.00m, updatedProduct.Price);
        Assert.Equal(tenant1, updatedProduct.TenantId);
    }

    [Fact]
    public async Task DeleteProduct_WithTenantHeader_ShouldDeleteOnlyTenantProduct()
    {
        // Arrange
        const string tenantId = "tenant1";

        await _factory.SeedDataAsync(tenantId, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminator_Product { Id = 1, Name = "Product To Delete", Price = 100, Category = "Electronics" });
            await context.SaveChangesAsync();
        });

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage deleteResponse = await _client.DeleteAsync("/api/discriminator/products/1");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deletion
        HttpResponseMessage getResponse = await _client.GetAsync("/api/discriminator/products/1");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithTenantHeader_ShouldCreateTenantSpecificOrder()
    {
        // Arrange
        const string tenantId = "tenant1";

        // First create some products
        await _factory.SeedDataAsync(tenantId, async context =>
        {
            context.Products.AddRange(
                new MultiTenantByDiscriminator_Product { Id = 1, Name = "Product 1", Price = 10.00m, Category = "Electronics" },
                new MultiTenantByDiscriminator_Product { Id = 2, Name = "Product 2", Price = 20.00m, Category = "Books" }
            );
            await context.SaveChangesAsync();
        });

        MultiTenantByDiscriminator_CreateOrderRequest orderRequest = new()
        {
            CustomerName = "Test Customer",
            Items = new List<MultiTenantByDiscriminator_CreateOrderItemRequest>
        {
            new() { ProductId = 1, Quantity = 2 },
            new() { ProductId = 2, Quantity = 1 }
        }
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/discriminator/orders", orderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminator_Order? createdOrder = JsonSerializer.Deserialize<MultiTenantByDiscriminator_Order>(content, _jsonOptions);

        Assert.NotNull(createdOrder);
        Assert.Equal(tenantId, createdOrder.TenantId);
        Assert.Equal("Test Customer", createdOrder.CustomerName);
        Assert.Equal(2, createdOrder.Items.Count);
        Assert.Equal(40.00m, createdOrder.Total); // (2 * 10) + (1 * 20)

        // Verify all items have correct tenant
        Assert.All(createdOrder.Items, item => Assert.Equal(tenantId, item.TenantId));
    }

    [Fact]
    public async Task GetOrders_WithDifferentTenants_ShouldReturnIsolatedData()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        // Create orders for both tenants
        await _factory.SeedDataAsync(tenant1, async context =>
        {
            await MultiTenantByDiscriminator_DataSeeder.SeedBasicProductsAsync(context);
            await MultiTenantByDiscriminator_DataSeeder.SeedOrdersAsync(context);
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            await MultiTenantByDiscriminator_DataSeeder.SeedBasicProductsAsync(context);
            await MultiTenantByDiscriminator_DataSeeder.SeedOrdersAsync(context);
        });

        // Act - Get orders for each tenant
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage tenant1Response = await _client.GetAsync("/api/discriminator/orders");
        string tenant1Content = await tenant1Response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminator_Order[]? tenant1Orders = JsonSerializer.Deserialize<MultiTenantByDiscriminator_Order[]>(tenant1Content, _jsonOptions);

        _client.SetTenantHeader(tenant2);
        HttpResponseMessage tenant2Response = await _client.GetAsync("/api/discriminator/orders");
        string tenant2Content = await tenant2Response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminator_Order[]? tenant2Orders = JsonSerializer.Deserialize<MultiTenantByDiscriminator_Order[]>(tenant2Content, _jsonOptions);

        // Assert
        Assert.NotNull(tenant1Orders);
        Assert.NotNull(tenant2Orders);

        Assert.All(tenant1Orders, order => Assert.Equal(tenant1, order.TenantId));
        Assert.All(tenant2Orders, order => Assert.Equal(tenant2, order.TenantId));

        // Data should be isolated
        Assert.DoesNotContain(tenant1Orders, order => order.TenantId == tenant2);
        Assert.DoesNotContain(tenant2Orders, order => order.TenantId == tenant1);
    }
}
