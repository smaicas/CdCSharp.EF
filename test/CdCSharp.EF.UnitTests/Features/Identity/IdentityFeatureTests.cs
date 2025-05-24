using CdCSharp.EF.Features.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdCSharp.EF.UnitTests.Features.Identity
{
    public class IdentityFeatureTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            IdentityFeature feature = new();

            // Assert
            Assert.False(feature.Enabled);
            Assert.NotNull(feature.Configuration);
            Assert.Equal(typeof(IdentityUser<Guid>), feature.UserType);
            Assert.Equal(typeof(IdentityRole<Guid>), feature.RoleType);
            Assert.Equal(typeof(IdentityUserClaim<Guid>), feature.UserClaimType);
            Assert.Equal(typeof(IdentityUserRole<Guid>), feature.UserRoleType);
            Assert.Equal(typeof(IdentityUserLogin<Guid>), feature.UserLoginType);
            Assert.Equal(typeof(IdentityRoleClaim<Guid>), feature.RoleClaimType);
            Assert.Equal(typeof(IdentityUserToken<Guid>), feature.UserTokenType);
        }

        [Fact]
        public void Configuration_CanBeModified()
        {
            // Arrange
            IdentityFeature feature = new();
            IdentityConfiguration newConfig = new()
            {
                UsersTableName = "CustomUsers",
                RolesTableName = "CustomRoles"
            };

            // Act
            feature.Configuration = newConfig;

            // Assert
            Assert.Equal("CustomUsers", feature.Configuration.UsersTableName);
            Assert.Equal("CustomRoles", feature.Configuration.RolesTableName);
        }

        [Fact]
        public void Types_CanBeCustomized()
        {
            // Arrange
            IdentityFeature feature = new();

            // Act
            feature.UserType = typeof(IdentityUser<int>);
            feature.RoleType = typeof(IdentityRole<int>);
            feature.UserClaimType = typeof(IdentityUserClaim<int>);
            feature.UserRoleType = typeof(IdentityUserRole<int>);
            feature.UserLoginType = typeof(IdentityUserLogin<int>);
            feature.RoleClaimType = typeof(IdentityRoleClaim<int>);
            feature.UserTokenType = typeof(IdentityUserToken<int>);

            // Assert
            Assert.Equal(typeof(IdentityUser<int>), feature.UserType);
            Assert.Equal(typeof(IdentityRole<int>), feature.RoleType);
            Assert.Equal(typeof(IdentityUserClaim<int>), feature.UserClaimType);
            Assert.Equal(typeof(IdentityUserRole<int>), feature.UserRoleType);
            Assert.Equal(typeof(IdentityUserLogin<int>), feature.UserLoginType);
            Assert.Equal(typeof(IdentityRoleClaim<int>), feature.RoleClaimType);
            Assert.Equal(typeof(IdentityUserToken<int>), feature.UserTokenType);
        }

        [Fact]
        public void Enabled_CanBeToggled()
        {
            // Arrange
            IdentityFeature feature = new();

            // Act & Assert
            Assert.False(feature.Enabled);

            feature.Enabled = true;
            Assert.True(feature.Enabled);

            feature.Enabled = false;
            Assert.False(feature.Enabled);
        }
    }
}
