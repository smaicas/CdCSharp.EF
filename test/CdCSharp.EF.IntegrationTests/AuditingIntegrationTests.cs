using CdCSharp.EF.IntegrationTests.Env;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CdCSharp.EF.IntegrationTests;

public class AuditingIntegrationTests : IClassFixture<DatabaseMultiTenantWebApplicationFactory>
{
    private readonly DatabaseMultiTenantWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuditingIntegrationTests()
    {
        _factory = new DatabaseMultiTenantWebApplicationFactory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    [Fact]
    public async Task CreateProduct_WithMultiTenantAndAuditing_SetsAllFields()
    {
        // Arrange
        const string tenantId = "tenant1";
        CreateProductRequest request = new()
        {
            Name = "Audited Tenant Product",
            Price = 150.00m,
            Category = "Electronics"
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        TestProduct? createdProduct = JsonSerializer.Deserialize<TestProduct>(content, _jsonOptions);

        Assert.NotNull(createdProduct);
        Assert.Equal(tenantId, createdProduct.TenantId); // Multi-tenant
        Assert.Equal(request.Name, createdProduct.Name);
        Assert.True(createdProduct.CreatedDate > DateTime.MinValue); // Auditing
        Assert.True(createdProduct.LastModifiedDate > DateTime.MinValue);
        Assert.Equal("SYSTEM", createdProduct.CreatedBy); // Default user
        Assert.Equal("SYSTEM", createdProduct.ModifiedBy);
    }

    [Fact]
    public async Task UpdateProduct_WithMultiTenantAndAuditing_UpdatesAuditFields()
    {
        // Arrange
        const string tenantId = "tenant1";

        // Create initial product
        await _factory.SeedDataForTenantAsync(tenantId, async context =>
        {
            context.Products.Add(new TestProduct
            {
                Id = 1,
                Name = "Original Product",
                Price = 100,
                Category = "Electronics",
                TenantId = tenantId,
                CreatedDate = DateTime.UtcNow.AddHours(-1),
                LastModifiedDate = DateTime.UtcNow.AddHours(-1),
                CreatedBy = "ORIGINAL",
                ModifiedBy = "ORIGINAL"
            });
            await context.SaveChangesAsync();
        });

        CreateProductRequest updateRequest = new()
        {
            Name = "Updated Product",
            Price = 200.00m,
            Category = "Updated Category"
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/products/1", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        TestProduct? updatedProduct = JsonSerializer.Deserialize<TestProduct>(content, _jsonOptions);

        Assert.NotNull(updatedProduct);
        Assert.Equal(tenantId, updatedProduct.TenantId);
        Assert.Equal(updateRequest.Name, updatedProduct.Name);
        Assert.Equal("SYSTEM", updatedProduct.CreatedBy); // Should change because strategy UseDefaultUser
        Assert.Equal("SYSTEM", updatedProduct.ModifiedBy); // Should change because strategy UseDefaultUser
        Assert.True(updatedProduct.LastModifiedDate > updatedProduct.CreatedDate);
    }

    [Fact]
    public async Task CreateOrder_WithAuditing_SetsAuditFieldsOnOrderOnly()
    {
        // Arrange
        const string tenantId = "tenant1";

        // Create products first
        await _factory.SeedDataForTenantAsync(tenantId, async context =>
        {
            context.Products.AddRange(
                new TestProduct { Id = 1, Name = "Product 1", Price = 25.00m, Category = "Electronics", TenantId = tenantId },
                new TestProduct { Id = 2, Name = "Product 2", Price = 50.00m, Category = "Books", TenantId = tenantId }
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

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/orders", orderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        TestOrder? createdOrder = JsonSerializer.Deserialize<TestOrder>(content, _jsonOptions);

        Assert.NotNull(createdOrder);
        Assert.Equal(tenantId, createdOrder.TenantId);
        Assert.Equal("Test Customer", createdOrder.CustomerName);

        // Verify auditing on order (implements IAuditableEntity)
        Assert.True(createdOrder.CreatedDate > DateTime.MinValue);
        Assert.True(createdOrder.LastModifiedDate > DateTime.MinValue);

        // Verify order items have tenant but no auditing (don't implement IAuditableEntity)
        Assert.All(createdOrder.Items, item => Assert.Equal(tenantId, item.TenantId));
    }
}
