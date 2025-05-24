using CdCSharp.EF.Features;
using Microsoft.AspNetCore.Identity;

namespace CdCSharp.EF.UnitTests.Features;

public class DbContextFeaturesBuilderTests
{
    [Fact]
    public void EnableAuditing_WhenCalled_EnablesAuditingFeature()
    {
        // Arrange
        DbContextFeaturesBuilder builder = new();

        // Act
        DbContextFeaturesBuilder result = builder.EnableAuditing();
        DbContextFeatures features = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.True(features.Auditing.Enabled);
    }

    [Fact]
    public void EnableIdentity_WithDefaultGenericType_EnablesIdentityFeature()
    {
        // Arrange
        DbContextFeaturesBuilder builder = new();

        // Act
        DbContextFeaturesBuilder result = builder.EnableIdentity<Guid>();
        DbContextFeatures features = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.True(features.Identity.Enabled);
        Assert.Equal(typeof(IdentityUser<Guid>), features.Identity.UserType);
        Assert.Equal(typeof(IdentityRole<Guid>), features.Identity.RoleType);
    }

    [Fact]
    public void EnableIdentity_WithIntKey_SetsCorrectTypes()
    {
        // Arrange
        DbContextFeaturesBuilder builder = new();

        // Act
        DbContextFeaturesBuilder result = builder.EnableIdentity<int>();
        DbContextFeatures features = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.True(features.Identity.Enabled);
        Assert.Equal(typeof(IdentityUser<int>), features.Identity.UserType);
        Assert.Equal(typeof(IdentityRole<int>), features.Identity.RoleType);
        Assert.Equal(typeof(IdentityUserClaim<int>), features.Identity.UserClaimType);
        Assert.Equal(typeof(IdentityUserRole<int>), features.Identity.UserRoleType);
        Assert.Equal(typeof(IdentityUserLogin<int>), features.Identity.UserLoginType);
        Assert.Equal(typeof(IdentityRoleClaim<int>), features.Identity.RoleClaimType);
        Assert.Equal(typeof(IdentityUserToken<int>), features.Identity.UserTokenType);
    }

    [Fact]
    public void EnableIdentity_WithCustomTypes_SetsCustomTypes()
    {
        // Arrange
        DbContextFeaturesBuilder builder = new();

        // Act
        DbContextFeaturesBuilder result = builder.EnableIdentity<string, CustomUser, CustomRole>();
        DbContextFeatures features = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.True(features.Identity.Enabled);
        Assert.Equal(typeof(CustomUser), features.Identity.UserType);
        Assert.Equal(typeof(CustomRole), features.Identity.RoleType);
    }

    [Fact]
    public void EnableIdentity_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        DbContextFeaturesBuilder builder = new();

        // Act
        DbContextFeaturesBuilder result = builder.EnableIdentity<Guid>(config =>
        {
            config.UsersTableName = "MyUsers";
            config.RolesTableName = "MyRoles";
        });
        DbContextFeatures features = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.True(features.Identity.Enabled);
        Assert.Equal("MyUsers", features.Identity.Configuration.UsersTableName);
        Assert.Equal("MyRoles", features.Identity.Configuration.RolesTableName);
    }

    [Fact]
    public void EnableIdentity_WithCustomTypesAndConfiguration_AppliesBoth()
    {
        // Arrange
        DbContextFeaturesBuilder builder = new();

        // Act
        DbContextFeaturesBuilder result = builder.EnableIdentity<string, CustomUser, CustomRole>(config =>
        {
            config.UsersTableName = "CustomUsersTable";
            config.Options.Password.RequiredLength = 10;
        });
        DbContextFeatures features = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.True(features.Identity.Enabled);
        Assert.Equal(typeof(CustomUser), features.Identity.UserType);
        Assert.Equal(typeof(CustomRole), features.Identity.RoleType);
        Assert.Equal("CustomUsersTable", features.Identity.Configuration.UsersTableName);
        Assert.Equal(10, features.Identity.Configuration.Options.Password.RequiredLength);
    }

    [Fact]
    public void EnableIdentity_CalledMultipleTimes_OverridesPreviousConfiguration()
    {
        // Arrange
        DbContextFeaturesBuilder builder = new();

        // Act
        builder.EnableIdentity<Guid>(config => config.UsersTableName = "FirstUsers");
        DbContextFeaturesBuilder result = builder.EnableIdentity<int>(config => config.UsersTableName = "SecondUsers");
        DbContextFeatures features = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.True(features.Identity.Enabled);
        Assert.Equal(typeof(IdentityUser<int>), features.Identity.UserType);
        Assert.Equal("SecondUsers", features.Identity.Configuration.UsersTableName);
    }

    [Fact]
    public void EnableAuditingAndIdentity_WhenBothCalled_EnablesBothFeatures()
    {
        // Arrange
        DbContextFeaturesBuilder builder = new();

        // Act
        DbContextFeaturesBuilder result = builder
            .EnableAuditing()
            .EnableIdentity<Guid>();
        DbContextFeatures features = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.True(features.Auditing.Enabled);
        Assert.True(features.Identity.Enabled);
    }

    [Fact]
    public void Build_WithoutEnablingFeatures_ReturnsDefaultFeatures()
    {
        // Arrange
        DbContextFeaturesBuilder builder = new();

        // Act
        DbContextFeatures features = builder.Build();

        // Assert
        Assert.False(features.Auditing.Enabled);
        Assert.False(features.Identity.Enabled);
    }

    // Custom classes for testing
    internal class CustomUser : IdentityUser<string>
    {
        public string CustomProperty { get; set; } = string.Empty;
    }

    internal class CustomRole : IdentityRole<string>
    {
        public string CustomRoleProperty { get; set; } = string.Empty;
    }
}
