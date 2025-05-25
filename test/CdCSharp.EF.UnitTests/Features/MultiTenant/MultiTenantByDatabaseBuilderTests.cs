using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Features.MultiTenant;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.UnitTests.Features.MultiTenant;

public class MultiTenantByDatabaseBuilderTests
{
    [Fact]
    public void AddTenant_WithValidConfiguration_AddsToBuilder()
    {
        // Arrange
        MultiTenantByDatabaseBuilder builder = new();

        // Act
        builder.AddTenant("tenant1", options => options.UseInMemoryDatabase("db1"));
        builder.AddTenant("tenant2", options => options.UseInMemoryDatabase("db2"));

        IDictionary<string, Action<DbContextOptionsBuilder>> result = builder.Build();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("tenant1"));
        Assert.True(result.ContainsKey("tenant2"));
    }

    [Fact]
    public void AddTenant_WithSameTenantId_OverwritesPreviousConfiguration()
    {
        // Arrange
        MultiTenantByDatabaseBuilder builder = new();

        // Act
        builder.AddTenant("tenant1", options => options.UseInMemoryDatabase("db1"));
        builder.AddTenant("tenant1", options => options.UseInMemoryDatabase("db2"));

        IDictionary<string, Action<DbContextOptionsBuilder>> result = builder.Build();

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey("tenant1"));
    }

    [Fact]
    public void Build_WithNoTenants_ReturnsEmptyDictionary()
    {
        // Arrange
        MultiTenantByDatabaseBuilder builder = new();

        // Act
        IDictionary<string, Action<DbContextOptionsBuilder>> result = builder.Build();

        // Assert
        Assert.Empty(result);
    }

    internal class MultiTenantByDatabaseBuilderTests_MultiTenantDbContext : ExtensibleDbContext
    {
        public MultiTenantByDatabaseBuilderTests_MultiTenantDbContext(DbContextOptions<MultiTenantByDatabaseBuilderTests_MultiTenantDbContext> options,
            IServiceProvider serviceProvider)
            : base(options, serviceProvider)
        {
        }

        public DbSet<MultiTenantByDatabaseBuilderTests_TenantEntity> Products { get; set; } = null!;
    }

    internal class MultiTenantByDatabaseBuilderTests_TenantEntity : ITenantEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string TenantId { get; set; } = string.Empty;
    }

}
