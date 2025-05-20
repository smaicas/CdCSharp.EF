using CdCSharp.EF.IntegrationTests.Env;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CdCSharp.EF.IntegrationTests;

public class DiscriminatorWithFeaturesIntegrationTests : IClassFixture<DiscriminatorWithFeaturesMultiTenantWebApplicationFactory>
{
    private readonly DiscriminatorWithFeaturesMultiTenantWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public DiscriminatorWithFeaturesIntegrationTests()
    {
        _factory = new DiscriminatorWithFeaturesMultiTenantWebApplicationFactory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    [Fact]
    public async Task CreateProduct_WithDiscriminatorAndAuditing_SetsAllFields()
    {
        // Arrange
        const string tenantId = "tenant1";
        CreateProductRequest request = new()
        {
            Name = "Discriminator Audited Product",
            Price = 125.00m,
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
        Assert.Equal(tenantId, createdProduct.TenantId); // Multi-tenant discriminator
        Assert.Equal(request.Name, createdProduct.Name);
        Assert.True(createdProduct.CreatedDate > DateTime.MinValue); // Auditing
        Assert.True(createdProduct.LastModifiedDate > DateTime.MinValue);
        Assert.Equal("SYSTEM", createdProduct.CreatedBy); // Default user
        Assert.Equal("SYSTEM", createdProduct.ModifiedBy);
    }

    [Fact]
    public async Task GetProducts_WithDifferentTenants_ReturnsIsolatedDataWithAuditing()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.Add(new TestProduct
            {
                Name = "T1 Product",
                Price = 100,
                Category = "Electronics",
                TenantId = tenant1,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow,
                CreatedBy = "SYSTEM",
                ModifiedBy = "SYSTEM"
            });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new TestProduct
            {
                Name = "T2 Product",
                Price = 200,
                Category = "Books",
                TenantId = tenant2,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow,
                CreatedBy = "SYSTEM",
                ModifiedBy = "SYSTEM"
            });
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

        Assert.Single(tenant1Products);
        Assert.Single(tenant2Products);

        // Verify tenant isolation
        Assert.Equal(tenant1, tenant1Products[0].TenantId);
        Assert.Equal(tenant2, tenant2Products[0].TenantId);

        // Verify auditing fields are present
        Assert.True(tenant1Products[0].CreatedDate > DateTime.MinValue);
        Assert.Equal("SYSTEM", tenant1Products[0].CreatedBy);
        Assert.True(tenant2Products[0].CreatedDate > DateTime.MinValue);
        Assert.Equal("SYSTEM", tenant2Products[0].CreatedBy);
    }

    [Fact]
    public async Task UpdateProduct_WithDiscriminatorAndAuditing_UpdatesAuditFields()
    {
        // Arrange
        const string tenantId = "tenant1";

        await _factory.SeedDataAsync(tenantId, async context =>
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
            Price = 150.00m,
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
}
