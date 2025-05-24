using CdCSharp.EF.Features.Identity;
using Microsoft.AspNetCore.Identity;

namespace CdCSharp.EF.UnitTests.Features.Identity;

public class IdentityConfigurationTests
{
    [Fact]
    public void Constructor_SetsDefaultTableNames()
    {
        // Act
        IdentityConfiguration config = new();

        // Assert
        Assert.Equal("AspNetUsers", config.UsersTableName);
        Assert.Equal("AspNetRoles", config.RolesTableName);
        Assert.Equal("AspNetUserClaims", config.UserClaimsTableName);
        Assert.Equal("AspNetUserRoles", config.UserRolesTableName);
        Assert.Equal("AspNetUserLogins", config.UserLoginsTableName);
        Assert.Equal("AspNetRoleClaims", config.RoleClaimsTableName);
        Assert.Equal("AspNetUserTokens", config.UserTokensTableName);
    }

    [Fact]
    public void Constructor_SetsDefaultIdentityOptions()
    {
        // Act
        IdentityConfiguration config = new();

        // Assert
        Assert.NotNull(config.Options);
        Assert.IsType<IdentityOptions>(config.Options);
    }

    [Fact]
    public void TableNames_CanBeCustomized()
    {
        // Arrange
        IdentityConfiguration config = new()
        {
            // Act
            UsersTableName = "MyUsers",
            RolesTableName = "MyRoles",
            UserClaimsTableName = "MyUserClaims",
            UserRolesTableName = "MyUserRoles",
            UserLoginsTableName = "MyUserLogins",
            RoleClaimsTableName = "MyRoleClaims",
            UserTokensTableName = "MyUserTokens"
        };

        // Assert
        Assert.Equal("MyUsers", config.UsersTableName);
        Assert.Equal("MyRoles", config.RolesTableName);
        Assert.Equal("MyUserClaims", config.UserClaimsTableName);
        Assert.Equal("MyUserRoles", config.UserRolesTableName);
        Assert.Equal("MyUserLogins", config.UserLoginsTableName);
        Assert.Equal("MyRoleClaims", config.RoleClaimsTableName);
        Assert.Equal("MyUserTokens", config.UserTokensTableName);
    }

    [Fact]
    public void Options_CanBeCustomized()
    {
        // Arrange
        IdentityConfiguration config = new();
        IdentityOptions customOptions = new()
        {
            Password = new PasswordOptions
            {
                RequiredLength = 8,
                RequireDigit = true
            }
        };

        // Act
        config.Options = customOptions;

        // Assert
        Assert.Equal(8, config.Options.Password.RequiredLength);
        Assert.True(config.Options.Password.RequireDigit);
    }
}
