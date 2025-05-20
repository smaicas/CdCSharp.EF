using CdCSharp.EF.IntegrationTests._Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CdCSharp.EF.IntegrationTests.MultiTenantByDatabaseWithAuditing;

public class MultiTenantByDatabaseWithAuditingIntTests : IDisposable
{
    private readonly MultiTenantByDatabaseWithAuditing_Factory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public MultiTenantByDatabaseWithAuditingIntTests()
    {
        _factory = new MultiTenantByDatabaseWithAuditing_Factory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task CreateProduct_WithMultiTenantAndAuditing_ShouldSetAllFields()
    {
        // Arrange
        const string tenantId = "tenant1";
        MultiTenantByDatabaseWithAuditing_CreateProductRequest request = new()
        {
            Name = "Audited Tenant Product",
            Price = 150.00m,
            Category = "Electronics"
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/database-auditing/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDatabaseWithAuditing_Product? createdProduct =
            JsonSerializer.Deserialize<MultiTenantByDatabaseWithAuditing_Product>(content, _jsonOptions);

        Assert.NotNull(createdProduct);
        Assert.Equal(tenantId, createdProduct.TenantId); // Multi-tenant
        Assert.Equal(request.Name, createdProduct.Name);
        Assert.True(createdProduct.CreatedDate > DateTime.MinValue); // Auditing
        Assert.True(createdProduct.LastModifiedDate > DateTime.MinValue);
        Assert.Equal("SYSTEM", createdProduct.CreatedBy); // Default user
        Assert.Equal("SYSTEM", createdProduct.ModifiedBy);
    }

    [Fact]
    public async Task UpdateProduct_WithMultiTenantAndAuditing_ShouldUpdateAuditFields()
    {
        // Arrange
        const string tenantId = "tenant1";

        // Create initial product
        await _factory.SeedDataForTenantAsync(tenantId, async context =>
        {
            context.Products.Add(new MultiTenantByDatabaseWithAuditing_Product
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

        MultiTenantByDatabaseWithAuditing_CreateProductRequest updateRequest = new()
        {
            Name = "Updated Product",
            Price = 200.00m,
            Category = "Updated Category"
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/database-auditing/products/1", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDatabaseWithAuditing_Product? updatedProduct =
            JsonSerializer.Deserialize<MultiTenantByDatabaseWithAuditing_Product>(content, _jsonOptions);

        Assert.NotNull(updatedProduct);
        Assert.Equal(tenantId, updatedProduct.TenantId);
        Assert.Equal(updateRequest.Name, updatedProduct.Name);
        Assert.Equal("SYSTEM", updatedProduct.CreatedBy); // Should change because strategy UseDefaultUser
        Assert.Equal("SYSTEM", updatedProduct.ModifiedBy); // Should change because strategy UseDefaultUser
        Assert.True(updatedProduct.LastModifiedDate > updatedProduct.CreatedDate);
    }

    [Fact]
    public async Task CreateOrder_WithAuditing_ShouldSetAuditFieldsOnOrderOnly()
    {
        // Arrange
        const string tenantId = "tenant1";

        // Create products first
        await _factory.SeedDataForTenantAsync(tenantId, async context =>
        {
            context.Products.AddRange(
                new MultiTenantByDatabaseWithAuditing_Product
                {
                    Id = 1,
                    Name = "Product 1",
                    Price = 25.00m,
                    Category = "Electronics",
                    TenantId = tenantId
                },
                new MultiTenantByDatabaseWithAuditing_Product
                {
                    Id = 2,
                    Name = "Product 2",
                    Price = 50.00m,
                    Category = "Books",
                    TenantId = tenantId
                }
            );
            await context.SaveChangesAsync();
        });

        MultiTenantByDatabaseWithAuditing_CreateOrderRequest orderRequest = new()
        {
            CustomerName = "Test Customer",
            Items = new List<MultiTenantByDatabaseWithAuditing_CreateOrderItemRequest>
        {
            new() { ProductId = 1, Quantity = 2 },
            new() { ProductId = 2, Quantity = 1 }
        }
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/database-auditing/orders", orderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDatabaseWithAuditing_Order? createdOrder =
            JsonSerializer.Deserialize<MultiTenantByDatabaseWithAuditing_Order>(content, _jsonOptions);

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
