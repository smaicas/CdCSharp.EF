using CdCSharp.EF.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.UnitTests;

public class MultiTenantByDatabaseBuilderTests
{
    [Fact]
    public void AddTenant_WithValidConfiguration_AddsToBuilder()
    {
        // Arrange
        MultiTenantByDatabaseBuilder<TestMultiTenantDbContext> builder = new();

        // Act
        builder.AddTenant("tenant1", options => options.UseInMemoryDatabase("db1"));
        builder.AddTenant("tenant2", options => options.UseInMemoryDatabase("db2"));

        IDictionary<string, Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TestMultiTenantDbContext>>> result = builder.Build();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("tenant1"));
        Assert.True(result.ContainsKey("tenant2"));
    }

    [Fact]
    public void AddTenant_WithSameTenantId_OverwritesPreviousConfiguration()
    {
        // Arrange
        MultiTenantByDatabaseBuilder<TestMultiTenantDbContext> builder = new();

        // Act
        builder.AddTenant("tenant1", options => options.UseInMemoryDatabase("db1"));
        builder.AddTenant("tenant1", options => options.UseInMemoryDatabase("db2"));

        IDictionary<string, Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TestMultiTenantDbContext>>> result = builder.Build();

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey("tenant1"));
    }

    [Fact]
    public void Build_WithNoTenants_ReturnsEmptyDictionary()
    {
        // Arrange
        MultiTenantByDatabaseBuilder<TestMultiTenantDbContext> builder = new();

        // Act
        IDictionary<string, Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TestMultiTenantDbContext>>> result = builder.Build();

        // Assert
        Assert.Empty(result);
    }
}
