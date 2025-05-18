using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CdCSharp.EF.UnitTests;

public class MultiTenantDbContextTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly Mock<ITenantStore> _mockTenantStore;
    private readonly ServiceProvider _serviceProvider;

    public MultiTenantDbContextTests()
    {
        _mockTenantStore = new Mock<ITenantStore>();

        ServiceCollection services = new();
        services.AddSingleton(_mockTenantStore.Object);
        _serviceProvider = services.BuildServiceProvider();

        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options, _serviceProvider);
    }

    [Fact]
    public void CurrentTenantId_WhenTenantStoreHasValue_ReturnsTenantId()
    {
        // Arrange
        const string expectedTenantId = "tenant1";
        _mockTenantStore.Setup(s => s.GetCurrentTenantId()).Returns(expectedTenantId);

        // Act
        string? result = _context.CurrentTenantId;

        // Assert
        Assert.Equal(expectedTenantId, result);
    }

    [Fact]
    public void CurrentTenantId_WhenTenantIdIsSetDirectly_ReturnsSetValue()
    {
        // Arrange
        const string expectedTenantId = "tenant1";
        _context.SetTenantId(expectedTenantId);

        // Act
        string? result = _context.CurrentTenantId;

        // Assert
        Assert.Equal(expectedTenantId, result);
    }

    [Fact]
    public void CurrentTenantId_WhenBothDirectAndStoreSet_ReturnsDirectValue()
    {
        // Arrange
        const string directTenantId = "direct-tenant";
        const string storeTenantId = "store-tenant";

        _mockTenantStore.Setup(s => s.GetCurrentTenantId()).Returns(storeTenantId);
        _context.SetTenantId(directTenantId);

        // Act
        string? result = _context.CurrentTenantId;

        // Assert
        Assert.Equal(directTenantId, result);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenAddingTenantEntity_SetsTenantId()
    {
        // Arrange
        const string tenantId = "tenant1";
        _mockTenantStore.Setup(s => s.GetCurrentTenantId()).Returns(tenantId);

        TestProduct product = new() { Name = "Test Product", Price = 100 };

        // Act
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Assert
        Assert.Equal(tenantId, product.TenantId);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenModifyingTenantEntity_UpdatesTenantId()
    {
        // Arrange
        const string originalTenantId = "tenant1";
        const string newTenantId = "tenant2";

        _mockTenantStore.Setup(s => s.GetCurrentTenantId()).Returns(originalTenantId);

        TestProduct product = new() { Name = "Test Product", Price = 100 };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Change tenant
        _mockTenantStore.Setup(s => s.GetCurrentTenantId()).Returns(newTenantId);

        // Act
        product.Name = "Updated Product";
        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        // Assert
        Assert.Equal(newTenantId, product.TenantId);
    }

    [Fact]
    public void SaveChanges_WhenAddingTenantEntity_SetsTenantId()
    {
        // Arrange
        const string tenantId = "tenant1";
        _mockTenantStore.Setup(s => s.GetCurrentTenantId()).Returns(tenantId);

        TestProduct product = new() { Name = "Test Product", Price = 100 };

        // Act
        _context.Products.Add(product);
        _context.SaveChanges();

        // Assert
        Assert.Equal(tenantId, product.TenantId);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNoTenantId_DoesNotSetTenantId()
    {
        // Arrange
        _mockTenantStore.Setup(s => s.GetCurrentTenantId()).Returns((string?)null);

        TestProduct product = new() { Name = "Test Product", Price = 100 };

        // Act
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Assert
        Assert.Empty(product.TenantId);
    }

    [Fact]
    public async Task Query_WithTenantFilter_FiltersCorrectly()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        // Add products for different tenants directly
        TestProduct product1 = new() { Name = "Product 1", Price = 100, TenantId = tenant1 };
        TestProduct product2 = new() { Name = "Product 2", Price = 200, TenantId = tenant2 };
        TestProduct product3 = new() { Name = "Product 3", Price = 300, TenantId = tenant1 };

        await _context.Database.EnsureCreatedAsync();
        _context.Products.AddRange(product1, product2, product3);
        await _context.SaveChangesAsync();

        // Set current tenant
        _mockTenantStore.Setup(s => s.GetCurrentTenantId()).Returns(tenant1);

        // Act
        List<TestProduct> products = await _context.Products.ToListAsync();

        // Assert
        Assert.Equal(2, products.Count);
        Assert.All(products, p => Assert.Equal(tenant1, p.TenantId));
    }

    [Fact]
    public async Task Query_WithDifferentTenant_ReturnsFilteredResults()
    {
        // Arrange
        const string tenant1 = "tenant1";
        const string tenant2 = "tenant2";

        // Add products for different tenants directly
        TestProduct product1 = new() { Name = "Product 1", Price = 100, TenantId = tenant1 };
        TestProduct product2 = new() { Name = "Product 2", Price = 200, TenantId = tenant2 };

        await _context.Database.EnsureCreatedAsync();
        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();

        // Set current tenant to tenant2
        _mockTenantStore.Setup(s => s.GetCurrentTenantId()).Returns(tenant2);

        // Act
        List<TestProduct> products = await _context.Products.ToListAsync();

        // Assert
        Assert.Single(products);
        Assert.Equal(tenant2, products[0].TenantId);
        Assert.Equal("Product 2", products[0].Name);
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }
}

// Test classes
public class TestDbContext : MultiTenantDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options, IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<TestProduct> Products { get; set; } = null!;

    // Make SetTenantId public for testing
    public new void SetTenantId(string tenantId) => base.SetTenantId(tenantId);
}

public class TestProduct : ITenantEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string TenantId { get; set; } = string.Empty;
}
