using CdCSharp.EF.IntegrationTests._Common;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDatabaseWithIdentity;

public class MultiTenantByDatabaseWithIdentityIntTests : IDisposable
{
    private readonly MultiTenantByDatabaseWithIdentity_Factory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public MultiTenantByDatabaseWithIdentityIntTests()
    {
        _factory = new MultiTenantByDatabaseWithIdentity_Factory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task CreateUser_WithMultiTenantAndIdentity_ShouldCreateInCorrectDatabase()
    {
        // Arrange
        const string tenantId = "tenant1";
        MultiTenantByDatabaseWithIdentity_CreateUserRequest request = new()
        {
            UserName = "tenant1user",
            Email = "tenant1@example.com"
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/database-identity/users", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        dynamic? createdUser = JsonSerializer.Deserialize<dynamic>(content, _jsonOptions);

        Assert.NotNull(createdUser);
    }

    [Fact]
    public async Task CreateProduct_WithMultiTenantAndIdentity_ShouldSetTenantId()
    {
        // Arrange
        const string tenantId = "tenant1";
        MultiTenantByDatabaseWithIdentity_CreateProductRequest request = new()
        {
            Name = "Identity Tenant Product",
            Price = 125.00m,
            Category = "Electronics"
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/database-identity/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDatabaseWithIdentity_Product? createdProduct =
            JsonSerializer.Deserialize<MultiTenantByDatabaseWithIdentity_Product>(content, _jsonOptions);

        Assert.NotNull(createdProduct);
        Assert.Equal(tenantId, createdProduct.TenantId);
        Assert.Equal(request.Name, createdProduct.Name);
    }

    [Fact]
    public async Task UsersAndProducts_WithDifferentTenants_ShouldBeIsolated()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        // Create users and products for tenant1
        await _factory.SeedDataForTenantAsync(tenant1, async context =>
        {
            context.Users.Add(new IdentityUser<Guid>
            {
                Id = Guid.NewGuid(),
                UserName = "tenant1user",
                Email = "tenant1@test.com",
                NormalizedUserName = "TENANT1USER",
                NormalizedEmail = "TENANT1@TEST.COM"
            });

            context.Products.Add(new MultiTenantByDatabaseWithIdentity_Product
            {
                Name = "T1 Product",
                Price = 100,
                Category = "Electronics",
                TenantId = tenant1
            });

            await context.SaveChangesAsync();
        });

        // Create users and products for tenant2
        await _factory.SeedDataForTenantAsync(tenant2, async context =>
        {
            context.Users.Add(new IdentityUser<Guid>
            {
                Id = Guid.NewGuid(),
                UserName = "tenant2user",
                Email = "tenant2@test.com",
                NormalizedUserName = "TENANT2USER",
                NormalizedEmail = "TENANT2@TEST.COM"
            });

            context.Products.Add(new MultiTenantByDatabaseWithIdentity_Product
            {
                Name = "T2 Product",
                Price = 200,
                Category = "Books",
                TenantId = tenant2
            });

            await context.SaveChangesAsync();
        });

        // Act - Get data for tenant1
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage tenant1UsersResponse = await _client.GetAsync("/api/database-identity/users");
        HttpResponseMessage tenant1ProductsResponse = await _client.GetAsync("/api/database-identity/products");

        // Get data for tenant2
        _client.SetTenantHeader(tenant2);
        HttpResponseMessage tenant2UsersResponse = await _client.GetAsync("/api/database-identity/users");
        HttpResponseMessage tenant2ProductsResponse = await _client.GetAsync("/api/database-identity/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, tenant1UsersResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tenant1ProductsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tenant2UsersResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tenant2ProductsResponse.StatusCode);

        // Verify data isolation
        string tenant1UsersContent = await tenant1UsersResponse.Content.ReadAsStringAsync();
        string tenant1ProductsContent = await tenant1ProductsResponse.Content.ReadAsStringAsync();
        string tenant2UsersContent = await tenant2UsersResponse.Content.ReadAsStringAsync();
        string tenant2ProductsContent = await tenant2ProductsResponse.Content.ReadAsStringAsync();

        dynamic[]? tenant1Users = JsonSerializer.Deserialize<dynamic[]>(tenant1UsersContent, _jsonOptions);
        MultiTenantByDatabaseWithIdentity_Product[]? tenant1Products = JsonSerializer.Deserialize<MultiTenantByDatabaseWithIdentity_Product[]>(tenant1ProductsContent, _jsonOptions);
        dynamic[]? tenant2Users = JsonSerializer.Deserialize<dynamic[]>(tenant2UsersContent, _jsonOptions);
        MultiTenantByDatabaseWithIdentity_Product[]? tenant2Products = JsonSerializer.Deserialize<MultiTenantByDatabaseWithIdentity_Product[]>(tenant2ProductsContent, _jsonOptions);

        Assert.NotNull(tenant1Users);
        Assert.NotNull(tenant1Products);
        Assert.NotNull(tenant2Users);
        Assert.NotNull(tenant2Products);

        Assert.Single(tenant1Users);
        Assert.Single(tenant1Products);
        Assert.Single(tenant2Users);
        Assert.Single(tenant2Products);

        Assert.Equal("T1 Product", tenant1Products[0].Name);
        Assert.Equal("T2 Product", tenant2Products[0].Name);
    }
}
