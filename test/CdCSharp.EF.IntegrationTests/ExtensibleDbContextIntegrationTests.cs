using CdCSharp.EF.IntegrationTests.Env;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CdCSharp.EF.IntegrationTests;

public class ExtensibleDbContextIntegrationTests : IClassFixture<ExtensibleDbContextWebApplicationFactory>
{
    private readonly ExtensibleDbContextWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExtensibleDbContextIntegrationTests()
    {
        _factory = new ExtensibleDbContextWebApplicationFactory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    [Fact]
    public async Task CreateProduct_WithAuditingEnabled_SetsAuditFields()
    {
        // Arrange
        CreateProductAuditableWithUserRequest request = new()
        {
            Name = "Audited Product",
            Price = 99.99m,
            Category = "Electronics"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/simple-products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        ProductAuditableWithUser? createdProduct = JsonSerializer.Deserialize<ProductAuditableWithUser>(content, _jsonOptions);

        Assert.NotNull(createdProduct);
        Assert.Equal(request.Name, createdProduct.Name);
        Assert.True(createdProduct.CreatedDate > DateTime.MinValue);
        Assert.True(createdProduct.LastModifiedDate > DateTime.MinValue);
        Assert.Equal("SYSTEM", createdProduct.CreatedBy); // Default user
        Assert.Equal("SYSTEM", createdProduct.ModifiedBy);
    }

    [Fact]
    public async Task UpdateProduct_WithAuditingEnabled_UpdatesLastModifiedDate()
    {
        // Arrange
        CreateProductAuditableWithUserRequest createRequest = new()
        {
            Name = "Original Product",
            Price = 50.00m,
            Category = "Books"
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/simple-products", createRequest);
        string createContent = await createResponse.Content.ReadAsStringAsync();
        ProductAuditableWithUser? originalProduct = JsonSerializer.Deserialize<ProductAuditableWithUser>(createContent, _jsonOptions);

        await Task.Delay(1000); // Ensure time difference

        CreateProductAuditableWithUserRequest updateRequest = new()
        {
            Name = "Updated Product",
            Price = 75.00m,
            Category = "Updated Category"
        };

        // Act
        HttpResponseMessage updateResponse = await _client.PutAsJsonAsync($"/api/simple-products/{originalProduct!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        string updateContent = await updateResponse.Content.ReadAsStringAsync();
        ProductAuditableWithUser? updatedProduct = JsonSerializer.Deserialize<ProductAuditableWithUser>(updateContent, _jsonOptions);

        Assert.NotNull(updatedProduct);
        Assert.Equal(updateRequest.Name, updatedProduct.Name);
        Assert.Equal(originalProduct.CreatedDate, updatedProduct.CreatedDate); // Should not change
        Assert.True(updatedProduct.LastModifiedDate > originalProduct.LastModifiedDate);
        Assert.Equal(originalProduct.CreatedBy, updatedProduct.CreatedBy); // Should not change
        Assert.Equal("SYSTEM", updatedProduct.ModifiedBy);
    }

    [Fact]
    public async Task GetProducts_WithoutTenant_ReturnsAllProducts()
    {
        // Arrange
        CreateProductAuditableWithUserRequest request1 = new() { Name = "Product 1", Price = 10, Category = "A" };
        CreateProductAuditableWithUserRequest request2 = new() { Name = "Product 2", Price = 20, Category = "B" };

        await _client.PostAsJsonAsync("/api/simple-products", request1);
        await _client.PostAsJsonAsync("/api/simple-products", request2);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/simple-products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        ProductAuditableWithUser[]? products = JsonSerializer.Deserialize<ProductAuditableWithUser[]>(content, _jsonOptions);

        Assert.NotNull(products);
        Assert.True(products.Length >= 2);
        Assert.Contains(products, p => p.Name == "Product 1");
        Assert.Contains(products, p => p.Name == "Product 2");
    }
}
