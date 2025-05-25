using CdCSharp.EF.Core;
using CdCSharp.EF.Features.MultiTenant;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.UnitTests.Features.MultiTenant;

public class MultiTenantConfigurationTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        MultiTenantConfiguration configuration = new();

        // Assert
        Assert.Equal(default, configuration.Strategy);
        Assert.Null(configuration.DiscriminatorConfiguration);
        Assert.NotNull(configuration.DatabaseConfigurations);
        Assert.Empty(configuration.DatabaseConfigurations);
    }

    [Fact]
    public void Strategy_CanBeSet()
    {
        // Arrange
        MultiTenantConfiguration configuration = new()
        {
            // Act
            Strategy = MultiTenantStrategy.Database
        };

        // Assert
        Assert.Equal(MultiTenantStrategy.Database, configuration.Strategy);

        // Act
        configuration.Strategy = MultiTenantStrategy.Discriminator;

        // Assert
        Assert.Equal(MultiTenantStrategy.Discriminator, configuration.Strategy);
    }

    [Fact]
    public void DiscriminatorConfiguration_CanBeSet()
    {
        // Arrange
        MultiTenantConfiguration configuration = new();
        Action<DbContextOptionsBuilder> configAction =
            options => options.UseInMemoryDatabase("test");

        // Act
        configuration.DiscriminatorConfiguration = configAction;

        // Assert
        Assert.Same(configAction, configuration.DiscriminatorConfiguration);
    }

    [Fact]
    public void DatabaseConfigurations_CanBeModified()
    {
        // Arrange
        MultiTenantConfiguration configuration = new();
        Action<DbContextOptionsBuilder> tenant1Config =
            options => options.UseInMemoryDatabase("tenant1");
        Action<DbContextOptionsBuilder> tenant2Config =
            options => options.UseInMemoryDatabase("tenant2");

        // Act
        configuration.DatabaseConfigurations["tenant1"] = tenant1Config;
        configuration.DatabaseConfigurations["tenant2"] = tenant2Config;

        // Assert
        Assert.Equal(2, configuration.DatabaseConfigurations.Count);
        Assert.Same(tenant1Config, configuration.DatabaseConfigurations["tenant1"]);
        Assert.Same(tenant2Config, configuration.DatabaseConfigurations["tenant2"]);
    }

    [Fact]
    public void DatabaseConfigurations_AllowsOverwriting()
    {
        // Arrange
        MultiTenantConfiguration configuration = new();
        Action<DbContextOptionsBuilder> originalConfig =
            options => options.UseInMemoryDatabase("original");
        Action<DbContextOptionsBuilder> newConfig =
            options => options.UseInMemoryDatabase("new");

        // Act
        configuration.DatabaseConfigurations["tenant1"] = originalConfig;
        configuration.DatabaseConfigurations["tenant1"] = newConfig;

        // Assert
        Assert.Single(configuration.DatabaseConfigurations);
        Assert.Same(newConfig, configuration.DatabaseConfigurations["tenant1"]);
    }

    [Fact]
    public void DatabaseConfigurations_SupportsMultipleTenants()
    {
        // Arrange
        MultiTenantConfiguration configuration = new();

        // Act
        for (int i = 1; i <= 10; i++)
        {
            string tenantId = $"tenant{i}";
            configuration.DatabaseConfigurations[tenantId] = options =>
                options.UseInMemoryDatabase($"db-{tenantId}");
        }

        // Assert
        Assert.Equal(10, configuration.DatabaseConfigurations.Count);
        Assert.All(configuration.DatabaseConfigurations.Keys, key => key.StartsWith("tenant"));
    }

    [Fact]
    public void Configuration_CanBeCombinedWithBothStrategies()
    {
        // Arrange
        MultiTenantConfiguration discriminatorConfig = new()
        {
            Strategy = MultiTenantStrategy.Discriminator,
            DiscriminatorConfiguration = options => options.UseInMemoryDatabase("shared")
        };

        MultiTenantConfiguration databaseConfig = new()
        {
            Strategy = MultiTenantStrategy.Database
        };
        databaseConfig.DatabaseConfigurations["tenant1"] = options => options.UseInMemoryDatabase("tenant1");

        // Assert
        Assert.Equal(MultiTenantStrategy.Discriminator, discriminatorConfig.Strategy);
        Assert.NotNull(discriminatorConfig.DiscriminatorConfiguration);
        Assert.Empty(discriminatorConfig.DatabaseConfigurations);

        Assert.Equal(MultiTenantStrategy.Database, databaseConfig.Strategy);
        Assert.Null(databaseConfig.DiscriminatorConfiguration);
        Assert.Single(databaseConfig.DatabaseConfigurations);
    }

    internal class MultiTenantConfigurationTests_DbContext : ExtensibleDbContext
    {
        public MultiTenantConfigurationTests_DbContext(
            DbContextOptions<MultiTenantConfigurationTests_DbContext> options,
            IServiceProvider serviceProvider) : base(options, serviceProvider)
        {
        }
    }
}
