using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CdCSharp.EF.IntegrationTests.Int_ExtensibleDbContextWithIdentity;

public class ExtensibleDbContextWithIdentityIntTests : IDisposable
{
    private readonly ExtensibleDbContextWithIdentity_Factory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExtensibleDbContextWithIdentityIntTests()
    {
        _factory = new ExtensibleDbContextWithIdentity_Factory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task CreateUser_WithIdentityEnabled_ShouldCreateUser()
    {
        // Arrange
        ExtensibleDbContextWithIdentity_CreateUserRequest request = new()
        {
            UserName = "testuser",
            Email = "test@example.com"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/extensible-identity/users", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        dynamic? createdUser = JsonSerializer.Deserialize<dynamic>(content, _jsonOptions);

        Assert.NotNull(createdUser);
        // Note: Dynamic deserialization for simplicity in tests
    }

    [Fact]
    public async Task CreateRole_WithIdentityEnabled_ShouldCreateRole()
    {
        // Arrange
        ExtensibleDbContextWithIdentity_CreateRoleRequest request = new()
        {
            Name = "Administrator"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/extensible-identity/roles", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        dynamic? createdRole = JsonSerializer.Deserialize<dynamic>(content, _jsonOptions);

        Assert.NotNull(createdRole);
    }

    [Fact]
    public async Task GetUsers_WithIdentityEnabled_ShouldReturnUsers()
    {
        // Arrange
        ExtensibleDbContextWithIdentity_CreateUserRequest user1 = new() { UserName = "user1", Email = "user1@test.com" };
        ExtensibleDbContextWithIdentity_CreateUserRequest user2 = new() { UserName = "user2", Email = "user2@test.com" };

        await _client.PostAsJsonAsync("/api/extensible-identity/users", user1);
        await _client.PostAsJsonAsync("/api/extensible-identity/users", user2);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/extensible-identity/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        dynamic[]? users = JsonSerializer.Deserialize<dynamic[]>(content, _jsonOptions);

        Assert.NotNull(users);
        Assert.True(users.Length >= 2);
    }

    [Fact]
    public async Task GetRoles_WithIdentityEnabled_ShouldReturnRoles()
    {
        // Arrange
        ExtensibleDbContextWithIdentity_CreateRoleRequest role1 = new() { Name = "Admin" };
        ExtensibleDbContextWithIdentity_CreateRoleRequest role2 = new() { Name = "User" };

        await _client.PostAsJsonAsync("/api/extensible-identity/roles", role1);
        await _client.PostAsJsonAsync("/api/extensible-identity/roles", role2);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/extensible-identity/roles");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        dynamic[]? roles = JsonSerializer.Deserialize<dynamic[]>(content, _jsonOptions);

        Assert.NotNull(roles);
        Assert.True(roles.Length >= 2);
    }

    [Fact]
    public async Task CreateProduct_WithIdentityEnabled_ShouldWorkNormally()
    {
        // Arrange
        ExtensibleDbContextWithIdentity_CreateProductRequest request = new()
        {
            Name = "Identity Test Product",
            Price = 99.99m,
            Category = "Test"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/extensible-identity/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        ExtensibleDbContextWithIdentity_Product? createdProduct =
            JsonSerializer.Deserialize<ExtensibleDbContextWithIdentity_Product>(content, _jsonOptions);

        Assert.NotNull(createdProduct);
        Assert.Equal(request.Name, createdProduct.Name);
        Assert.Equal(request.Price, createdProduct.Price);
    }

    [Fact]
    public async Task IdentityAndRegularEntities_ShouldCoexist()
    {
        // Arrange
        ExtensibleDbContextWithIdentity_CreateUserRequest userRequest = new()
        {
            UserName = "coexistuser",
            Email = "coexist@test.com"
        };

        ExtensibleDbContextWithIdentity_CreateProductRequest productRequest = new()
        {
            Name = "Coexist Product",
            Price = 50.00m,
            Category = "Test"
        };

        // Act
        HttpResponseMessage userResponse = await _client.PostAsJsonAsync("/api/extensible-identity/users", userRequest);
        HttpResponseMessage productResponse = await _client.PostAsJsonAsync("/api/extensible-identity/products", productRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, userResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        // Verify both can be retrieved
        HttpResponseMessage getUsersResponse = await _client.GetAsync("/api/extensible-identity/users");
        HttpResponseMessage getProductsResponse = await _client.GetAsync("/api/extensible-identity/products");

        Assert.Equal(HttpStatusCode.OK, getUsersResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getProductsResponse.StatusCode);
    }
}
