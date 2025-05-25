using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CdCSharp.EF.IntegrationTests.Int_ExtensibleDbContext;

public class ExtensibleDbContextIntTests : IDisposable
{
    private readonly ExtensibleDbContext_Factory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExtensibleDbContextIntTests()
    {
        _factory = new ExtensibleDbContext_Factory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task CreateProduct_WithoutAuditingEnabled_ShouldNotSetAuditFields()
    {
        // Arrange
        ExtensibleDbContext_CreateProductRequest request = new()
        {
            Name = "Audited Product",
            Price = 99.99m,
            Category = "Electronics"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/extensible/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        ExtensibleDbContext_Product? createdProduct = JsonSerializer.Deserialize<ExtensibleDbContext_Product>(content, _jsonOptions);

        Assert.NotNull(createdProduct);
        Assert.Equal(request.Name, createdProduct.Name);
        Assert.Equal(default(DateTime), createdProduct.CreatedDate);
        Assert.Equal(default(DateTime), createdProduct.LastModifiedDate);
        Assert.Null(createdProduct.CreatedBy);
        Assert.Null(createdProduct.ModifiedBy);
    }

    [Fact]
    public async Task UpdateProduct_WithoutAuditingEnabled_ShouldNotUpdateLastModifiedDate()
    {
        // Arrange
        ExtensibleDbContext_CreateProductRequest createRequest = new()
        {
            Name = "Original Product",
            Price = 50.00m,
            Category = "Books"
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/extensible/products", createRequest);
        string createContent = await createResponse.Content.ReadAsStringAsync();
        ExtensibleDbContext_Product? originalProduct = JsonSerializer.Deserialize<ExtensibleDbContext_Product>(createContent, _jsonOptions);

        await Task.Delay(1000); // Ensure time difference

        ExtensibleDbContext_CreateProductRequest updateRequest = new()
        {
            Name = "Updated Product",
            Price = 75.00m,
            Category = "Updated Category"
        };

        // Act
        HttpResponseMessage updateResponse = await _client.PutAsJsonAsync($"/api/extensible/products/{originalProduct!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        string updateContent = await updateResponse.Content.ReadAsStringAsync();
        ExtensibleDbContext_Product? updatedProduct = JsonSerializer.Deserialize<ExtensibleDbContext_Product>(updateContent, _jsonOptions);

        Assert.NotNull(updatedProduct);
        Assert.Equal(updateRequest.Name, updatedProduct.Name);
        Assert.Equal(originalProduct.CreatedDate, updatedProduct.CreatedDate); // Should not change
        Assert.Equal(default(DateTime), originalProduct.LastModifiedDate);
        Assert.Equal(originalProduct.CreatedBy, updatedProduct.CreatedBy); // Should not change
        Assert.Null(updatedProduct.ModifiedBy);
    }

    [Fact]
    public async Task GetProducts_WithoutTenant_ShouldReturnAllProducts()
    {
        // Arrange
        ExtensibleDbContext_CreateProductRequest request1 = new() { Name = "Product 1", Price = 10, Category = "A" };
        ExtensibleDbContext_CreateProductRequest request2 = new() { Name = "Product 2", Price = 20, Category = "B" };

        await _client.PostAsJsonAsync("/api/extensible/products", request1);
        await _client.PostAsJsonAsync("/api/extensible/products", request2);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/extensible/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        ExtensibleDbContext_Product[]? products = JsonSerializer.Deserialize<ExtensibleDbContext_Product[]>(content, _jsonOptions);

        Assert.NotNull(products);
        Assert.True(products.Length >= 2);
        Assert.Contains(products, p => p.Name == "Product 1");
        Assert.Contains(products, p => p.Name == "Product 2");
    }
}
