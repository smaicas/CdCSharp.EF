using CdCSharp.EF.IntegrationTests._Common;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDiscriminatorWithIdentity;

public class MultiTenantByDiscriminatorWithIdentityIntTests : IDisposable
{
    private readonly MultiTenantByDiscriminatorWithIdentity_Factory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public MultiTenantByDiscriminatorWithIdentityIntTests()
    {
        _factory = new MultiTenantByDiscriminatorWithIdentity_Factory();
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task CreateProduct_WithDiscriminatorAndIdentity_ShouldSetTenantId()
    {
        // Arrange
        const string tenantId = "tenant1";
        MultiTenantByDiscriminatorWithIdentity_CreateProductRequest request = new()
        {
            Name = "Discriminator Identity Product",
            Price = 175.00m,
            Category = "Electronics"
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/discriminator-identity/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorWithIdentity_Product? createdProduct =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorWithIdentity_Product>(content, _jsonOptions);

        Assert.NotNull(createdProduct);
        Assert.Equal(tenantId, createdProduct.TenantId); // Multi-tenant discriminator
        Assert.Equal(request.Name, createdProduct.Name);
        Assert.Equal(request.Price, createdProduct.Price);
    }

    [Fact]
    public async Task CreateUser_WithDiscriminatorAndIdentity_ShouldCreateUser()
    {
        // Arrange
        const string tenantId = "tenant1";
        MultiTenantByDiscriminatorWithIdentity_CreateUserRequest request = new()
        {
            UserName = "discriminatoruser",
            Email = "discriminator@example.com"
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/discriminator-identity/users", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        dynamic? createdUser = JsonSerializer.Deserialize<dynamic>(content, _jsonOptions);

        Assert.NotNull(createdUser);
    }

    [Fact]
    public async Task CreateRole_WithDiscriminatorAndIdentity_ShouldCreateRole()
    {
        // Arrange
        const string tenantId = "tenant1";
        MultiTenantByDiscriminatorWithIdentity_CreateRoleRequest request = new()
        {
            Name = "Manager"
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/discriminator-identity/roles", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        dynamic? createdRole = JsonSerializer.Deserialize<dynamic>(content, _jsonOptions);

        Assert.NotNull(createdRole);
    }

    [Fact]
    public async Task GetProducts_WithDifferentTenants_ShouldReturnIsolatedData()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminatorWithIdentity_Product
            {
                Name = "T1 Identity Product",
                Price = 100,
                Category = "Electronics",
                TenantId = tenant1
            });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Products.Add(new MultiTenantByDiscriminatorWithIdentity_Product
            {
                Name = "T2 Identity Product",
                Price = 200,
                Category = "Books",
                TenantId = tenant2
            });
            await context.SaveChangesAsync();
        });

        // Act - Get products for tenant1
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage tenant1Response = await _client.GetAsync("/api/discriminator-identity/products");
        string tenant1Content = await tenant1Response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorWithIdentity_Product[]? tenant1Products =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorWithIdentity_Product[]>(tenant1Content, _jsonOptions);

        // Get products for tenant2
        _client.SetTenantHeader(tenant2);
        HttpResponseMessage tenant2Response = await _client.GetAsync("/api/discriminator-identity/products");
        string tenant2Content = await tenant2Response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorWithIdentity_Product[]? tenant2Products =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorWithIdentity_Product[]>(tenant2Content, _jsonOptions);

        // Assert
        Assert.NotNull(tenant1Products);
        Assert.NotNull(tenant2Products);

        Assert.Single(tenant1Products);
        Assert.Single(tenant2Products);

        // Verify tenant isolation
        Assert.Equal(tenant1, tenant1Products[0].TenantId);
        Assert.Equal(tenant2, tenant2Products[0].TenantId);

        Assert.Equal("T1 Identity Product", tenant1Products[0].Name);
        Assert.Equal("T2 Identity Product", tenant2Products[0].Name);
    }

    [Fact]
    public async Task UsersAndProducts_WithSameTenant_ShouldShareSameDatabase()
    {
        // Arrange
        const string tenantId = "tenant1";

        // Seed data for the tenant
        await _factory.SeedDataAsync(tenantId, async context =>
        {
            await MultiTenantByDiscriminatorWithIdentity_DataSeeder.SeedBasicUsersAsync(context);
            await MultiTenantByDiscriminatorWithIdentity_DataSeeder.SeedBasicProductsAsync(context);
        });

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage usersResponse = await _client.GetAsync("/api/discriminator-identity/users");
        HttpResponseMessage productsResponse = await _client.GetAsync("/api/discriminator-identity/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, productsResponse.StatusCode);

        string usersContent = await usersResponse.Content.ReadAsStringAsync();
        string productsContent = await productsResponse.Content.ReadAsStringAsync();

        dynamic[]? users = JsonSerializer.Deserialize<dynamic[]>(usersContent, _jsonOptions);
        MultiTenantByDiscriminatorWithIdentity_Product[]? products = JsonSerializer.Deserialize<MultiTenantByDiscriminatorWithIdentity_Product[]>(productsContent, _jsonOptions);

        Assert.NotNull(users);
        Assert.NotNull(products);
        Assert.True(users.Length >= 2);
        Assert.True(products.Length >= 2);

        // All products should have the same tenant ID
        Assert.All(products, p => Assert.Equal(tenantId, p.TenantId));
    }

    [Fact]
    public async Task CreateOrder_WithDiscriminatorAndIdentity_ShouldCreateOrderWithItems()
    {
        // Arrange
        const string tenantId = "tenant1";

        // Create products first
        await _factory.SeedDataAsync(tenantId, async context =>
        {
            context.Products.AddRange(
                new MultiTenantByDiscriminatorWithIdentity_Product
                {
                    Id = 1,
                    Name = "Identity Product 1",
                    Price = 30.00m,
                    Category = "Electronics",
                    TenantId = tenantId
                },
                new MultiTenantByDiscriminatorWithIdentity_Product
                {
                    Id = 2,
                    Name = "Identity Product 2",
                    Price = 45.00m,
                    Category = "Books",
                    TenantId = tenantId
                }
            );
            await context.SaveChangesAsync();
        });

        MultiTenantByDiscriminatorWithIdentity_CreateOrderRequest orderRequest = new()
        {
            CustomerName = "Identity Customer",
            Items = new List<MultiTenantByDiscriminatorWithIdentity_CreateOrderItemRequest>
        {
            new() { ProductId = 1, Quantity = 3 },
            new() { ProductId = 2, Quantity = 2 }
        }
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/discriminator-identity/orders", orderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorWithIdentity_Order? createdOrder =
            JsonSerializer.Deserialize<MultiTenantByDiscriminatorWithIdentity_Order>(content, _jsonOptions);

        Assert.NotNull(createdOrder);
        Assert.Equal(tenantId, createdOrder.TenantId);
        Assert.Equal("Identity Customer", createdOrder.CustomerName);
        Assert.Equal(2, createdOrder.Items.Count);
        Assert.Equal(180.00m, createdOrder.Total); // (3 * 30) + (2 * 45)

        // Verify all items have correct tenant
        Assert.All(createdOrder.Items, item => Assert.Equal(tenantId, item.TenantId));
    }

    [Fact]
    public async Task GetUsers_WithDifferentTenants_ShouldShareSameIdentityTables()
    {
        // Arrange - In discriminator strategy, Identity tables are shared across tenants
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        // Create users for both tenants (they will be in the same table)
        await _factory.SeedDataAsync(tenant1, async context =>
        {
            context.Users.Add(new IdentityUser<Guid>
            {
                Id = Guid.NewGuid(),
                UserName = "tenant1user",
                Email = "tenant1@test.com",
                NormalizedUserName = "TENANT1USER",
                NormalizedEmail = "TENANT1@TEST.COM"
            });
            await context.SaveChangesAsync();
        });

        await _factory.SeedDataAsync(tenant2, async context =>
        {
            context.Users.Add(new IdentityUser<Guid>
            {
                Id = Guid.NewGuid(),
                UserName = "tenant2user",
                Email = "tenant2@test.com",
                NormalizedUserName = "TENANT2USER",
                NormalizedEmail = "TENANT2@TEST.COM"
            });
            await context.SaveChangesAsync();
        });

        // Act - Get users for tenant1 (should see all users since Identity tables are shared)
        _client.SetTenantHeader(tenant1);
        HttpResponseMessage tenant1Response = await _client.GetAsync("/api/discriminator-identity/users");

        // Get users for tenant2 (should see the same users)
        _client.SetTenantHeader(tenant2);
        HttpResponseMessage tenant2Response = await _client.GetAsync("/api/discriminator-identity/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, tenant1Response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tenant2Response.StatusCode);

        string tenant1Content = await tenant1Response.Content.ReadAsStringAsync();
        string tenant2Content = await tenant2Response.Content.ReadAsStringAsync();

        dynamic[]? tenant1Users = JsonSerializer.Deserialize<dynamic[]>(tenant1Content, _jsonOptions);
        dynamic[]? tenant2Users = JsonSerializer.Deserialize<dynamic[]>(tenant2Content, _jsonOptions);

        Assert.NotNull(tenant1Users);
        Assert.NotNull(tenant2Users);

        // In discriminator strategy, Identity tables are shared, so both tenants see all users
        Assert.Equal(tenant1Users.Length, tenant2Users.Length);
        Assert.True(tenant1Users.Length >= 2);
    }

    [Fact]
    public async Task IdentityAndMultiTenantEntities_ShouldCoexistCorrectly()
    {
        // Arrange
        const string tenantId = "coexist-tenant";

        MultiTenantByDiscriminatorWithIdentity_CreateUserRequest userRequest = new()
        {
            UserName = "coexistuser",
            Email = "coexist@test.com"
        };

        MultiTenantByDiscriminatorWithIdentity_CreateRoleRequest roleRequest = new()
        {
            Name = "CoexistRole"
        };

        MultiTenantByDiscriminatorWithIdentity_CreateProductRequest productRequest = new()
        {
            Name = "Coexist Product",
            Price = 89.99m,
            Category = "Test"
        };

        // Act
        _client.SetTenantHeader(tenantId);
        HttpResponseMessage userResponse = await _client.PostAsJsonAsync("/api/discriminator-identity/users", userRequest);
        HttpResponseMessage roleResponse = await _client.PostAsJsonAsync("/api/discriminator-identity/roles", roleRequest);
        HttpResponseMessage productResponse = await _client.PostAsJsonAsync("/api/discriminator-identity/products", productRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, userResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, roleResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        // Verify all can be retrieved
        HttpResponseMessage getUsersResponse = await _client.GetAsync("/api/discriminator-identity/users");
        HttpResponseMessage getRolesResponse = await _client.GetAsync("/api/discriminator-identity/roles");
        HttpResponseMessage getProductsResponse = await _client.GetAsync("/api/discriminator-identity/products");

        Assert.Equal(HttpStatusCode.OK, getUsersResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getRolesResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getProductsResponse.StatusCode);

        // Verify product has tenant isolation
        string productsContent = await getProductsResponse.Content.ReadAsStringAsync();
        MultiTenantByDiscriminatorWithIdentity_Product[]? products = JsonSerializer.Deserialize<MultiTenantByDiscriminatorWithIdentity_Product[]>(productsContent, _jsonOptions);

        Assert.NotNull(products);
        Assert.Contains(products, p => p.Name == "Coexist Product" && p.TenantId == tenantId);
    }
}
