using CdCSharp.EF.IntegrationTests._Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDiscriminatorWithAuditing;

public class MultiTenantByDiscriminatorWithAuditingIntTests : IDisposable
{
    private readonly MultiTenantByDiscriminatorWithAuditing_Factory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public MultiTenantByDiscriminatorWithAuditingIntTests()
    {
        _factory = new MultiTenantByDiscriminatorWithAuditing_Factory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task CreateProduct_WithDiscriminatorAndAuditing_ShouldSetAllFields()
    {
        // Arrange
        const string tenantId = "tenant1";
        MultiTenantByDiscriminatorWithAuditing_CreateProductRequest request = new()
        {
            Name = "Discriminator Audited Product",
            Price = 125.00m,
            Category = "Electronics"
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/discriminator-auditing/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorWithAuditing_Product? createdProduct =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorWithAuditing_Product>(content, _jsonOptions);

        Assert.NotNull(createdProduct);
        Assert.Equal(tenantId, createdProduct.TenantId); // Multi-tenant discriminator
        Assert.Equal(request.Name, createdProduct.Name);
        Assert.True(createdProduct.CreatedDate > DateTime.MinValue); // Auditing
        Assert.True(createdProduct.LastModifiedDate > DateTime.MinValue);
        Assert.Equal("SYSTEM", createdProduct.CreatedBy); // Default user
        Assert.Equal("SYSTEM", createdProduct.ModifiedBy);
    }

    [Fact]
    public async Task GetProducts_WithDifferentTenants_ShouldReturnIsolatedDataWithAuditing()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminatorWithAuditing_Product
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
            context.Products.Add(new MultiTenantByDiscriminatorWithAuditing_Product
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
        HttpResponseMessage tenant1Response = await _client.GetAsync("/api/discriminator-auditing/products");
        string tenant1Content = await tenant1Response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorWithAuditing_Product[]? tenant1Products =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorWithAuditing_Product[]>(tenant1Content, _jsonOptions);

        // Get products for tenant2
        _client.SetTenantHeader(tenant2);
        HttpResponseMessage tenant2Response = await _client.GetAsync("/api/discriminator-auditing/products");
        string tenant2Content = await tenant2Response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorWithAuditing_Product[]? tenant2Products =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorWithAuditing_Product[]>(tenant2Content, _jsonOptions);

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
    public async Task UpdateProduct_WithDiscriminatorAndAuditing_ShouldUpdateAuditFields()
    {
        // Arrange
        const string tenantId = "tenant1";

        await _factory.SeedDataAsync(tenantId, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminatorWithAuditing_Product
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

        MultiTenantByDiscriminatorWithAuditing_CreateProductRequest updateRequest = new()
        {
            Name = "Updated Product",
            Price = 150.00m,
            Category = "Updated Category"
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/discriminator-auditing/products/1", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorWithAuditing_Product? updatedProduct =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorWithAuditing_Product>(content, _jsonOptions);

        Assert.NotNull(updatedProduct);
        Assert.Equal(tenantId, updatedProduct.TenantId);
        Assert.Equal(updateRequest.Name, updatedProduct.Name);
        Assert.Equal("SYSTEM", updatedProduct.CreatedBy); // Should change because strategy UseDefaultUser
        Assert.Equal("SYSTEM", updatedProduct.ModifiedBy); // Should change because strategy UseDefaultUser
        Assert.True(updatedProduct.LastModifiedDate > updatedProduct.CreatedDate);
    }
}
