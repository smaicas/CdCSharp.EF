using CdCSharp.EF.Features;
using CdCSharp.EF.Features.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.UnitTests.Features.Identity;

// Custom Identity User with navigation properties
public class MyUser : IdentityUser<Guid>
{
    public virtual ICollection<MyUserRole> UserRoles { get; set; } = new List<MyUserRole>();
}

// Custom Identity Role with navigation properties
public class MyRole : IdentityRole<Guid>
{
}

// Custom Identity Claim, Login, Role, Token types
public class MyUserClaim : IdentityUserClaim<Guid> { public virtual MyUser User { get; set; } = default!; }
public class MyUserLogin : IdentityUserLogin<Guid> { public virtual MyUser User { get; set; } = default!; }
public class MyUserRole : IdentityUserRole<Guid> { public virtual MyUser User { get; set; } = default!; public virtual MyRole Role { get; set; } = default!; }
public class MyRoleClaim : IdentityRoleClaim<Guid> { public virtual MyRole Role { get; set; } = default!; }
public class MyUserToken : IdentityUserToken<Guid> { public virtual MyUser User { get; set; } = default!; }

public class IdentityFeatureProcessorWithDbSetTests : IDisposable
{
    private readonly IdentityFeatureProcessorWithDbSetTests_DbContext _context;
    private readonly IdentityFeatureProcessor _processor;
    private readonly ServiceProvider _serviceProvider;
    private readonly IdentityFeature _feature; // Now populated via DbContextFeaturesBuilder

    public IdentityFeatureProcessorWithDbSetTests()
    {
        ServiceCollection services = new();
        _serviceProvider = services.BuildServiceProvider();

        DbContextOptions<IdentityFeatureProcessorWithDbSetTests_DbContext> options = new DbContextOptionsBuilder<IdentityFeatureProcessorWithDbSetTests_DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new IdentityFeatureProcessorWithDbSetTests_DbContext(options);

        // Use the DbContextFeaturesBuilder to create the IdentityFeature instance
        // This is where compile-time type safety is enforced for MyUser and MyRole
        _feature = new DbContextFeaturesBuilder()
            .EnableIdentity<Guid, MyUser, MyRole, MyUserClaim, MyUserRole, MyUserLogin, MyRoleClaim, MyUserToken>(config =>
            {
                // You can optionally configure Identity settings here
                // config.UsersTableName = "MyUsers";
            })
            .Build()
            .Identity; // Get the IdentityFeature from the built DbContextFeatures

        _processor = new IdentityFeatureProcessor(_feature, _serviceProvider);
    }

    [Fact]
    public void OnModelCreating_WhenCalled_ConfiguresAllIdentityEntities()
    {
        // Arrange
        ModelBuilder modelBuilder = new();
        // Act: Directly call the processor's OnModelCreating
        _processor.OnModelCreating(modelBuilder);

        // Assert
        IMutableEntityType? userEntityType = modelBuilder.Model.FindEntityType(typeof(MyUser));
        IMutableEntityType? roleEntityType = modelBuilder.Model.FindEntityType(typeof(MyRole));
        IMutableEntityType? userClaimEntityType = modelBuilder.Model.FindEntityType(typeof(MyUserClaim));
        IMutableEntityType? userRoleEntityType = modelBuilder.Model.FindEntityType(typeof(MyUserRole));
        IMutableEntityType? userLoginEntityType = modelBuilder.Model.FindEntityType(typeof(MyUserLogin));
        IMutableEntityType? roleClaimEntityType = modelBuilder.Model.FindEntityType(typeof(MyRoleClaim));
        IMutableEntityType? userTokenEntityType = modelBuilder.Model.FindEntityType(typeof(MyUserToken));

        Assert.NotNull(userEntityType);
        Assert.NotNull(roleEntityType);
        Assert.NotNull(userClaimEntityType);
        Assert.NotNull(userRoleEntityType);
        Assert.NotNull(userLoginEntityType);
        Assert.NotNull(roleClaimEntityType);
        Assert.NotNull(userTokenEntityType);

        Assert.Equal("AspNetUsers", userEntityType.GetTableName());
        Assert.Equal("AspNetRoles", roleEntityType.GetTableName());
        Assert.Equal("AspNetUserClaims", userClaimEntityType.GetTableName());
        Assert.Equal("AspNetUserRoles", userRoleEntityType.GetTableName());
        Assert.Equal("AspNetUserLogins", userLoginEntityType.GetTableName());
        Assert.Equal("AspNetRoleClaims", roleClaimEntityType.GetTableName());
        Assert.Equal("AspNetUserTokens", userTokenEntityType.GetTableName());
    }

    [Fact]
    public void OnModelCreating_WithCustomTableNames_UsesCustomNames()
    {
        // Arrange
        // Create a feature with custom configuration using the builder
        IdentityFeature customFeature = new DbContextFeaturesBuilder()
            .EnableIdentity<Guid, MyUser, MyRole, MyUserClaim, MyUserRole, MyUserLogin, MyRoleClaim, MyUserToken>(config =>
            {
                config.UsersTableName = "CustomUsers";
                config.RolesTableName = "CustomRoles";
                config.UserClaimsTableName = "CustomUserClaims";
                config.UserRolesTableName = "CustomUserRoles";
                config.UserLoginsTableName = "CustomUserLogins";
                config.RoleClaimsTableName = "CustomRoleClaims";
                config.UserTokensTableName = "CustomUserTokens";
            })
            .Build()
            .Identity;

        IdentityFeatureProcessor processor = new(customFeature, _serviceProvider);
        ModelBuilder modelBuilder = new();

        // Act
        processor.OnModelCreating(modelBuilder);

        // Assert
        IMutableEntityType? userEntityType = modelBuilder.Model.FindEntityType(typeof(MyUser));
        IMutableEntityType? roleEntityType = modelBuilder.Model.FindEntityType(typeof(MyRole));
        IMutableEntityType? userClaimEntityType = modelBuilder.Model.FindEntityType(typeof(MyUserClaim));
        IMutableEntityType? userRoleEntityType = modelBuilder.Model.FindEntityType(typeof(MyUserRole));
        IMutableEntityType? userLoginEntityType = modelBuilder.Model.FindEntityType(typeof(MyUserLogin));
        IMutableEntityType? roleClaimEntityType = modelBuilder.Model.FindEntityType(typeof(MyRoleClaim));
        IMutableEntityType? userTokenEntityType = modelBuilder.Model.FindEntityType(typeof(MyUserToken));

        Assert.Equal("CustomUsers", userEntityType?.GetTableName());
        Assert.Equal("CustomRoles", roleEntityType?.GetTableName());
        Assert.Equal("CustomUserClaims", userClaimEntityType?.GetTableName());
        Assert.Equal("CustomUserRoles", userRoleEntityType?.GetTableName());
        Assert.Equal("CustomUserLogins", userLoginEntityType?.GetTableName());
        Assert.Equal("CustomRoleClaims", roleClaimEntityType?.GetTableName());
        Assert.Equal("CustomUserTokens", userTokenEntityType?.GetTableName());
    }

    [Fact]
    public void OnModelCreating_WhenCalled_ConfiguresUserEntityCorrectly()
    {
        // Arrange
        ModelBuilder modelBuilder = new();

        // Act
        _processor.OnModelCreating(modelBuilder);

        // Assert
        IMutableEntityType? userEntityType = modelBuilder.Model.FindEntityType(typeof(MyUser));
        Assert.NotNull(userEntityType);

        IMutableKey? primaryKey = userEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal("Id", primaryKey.Properties.First().Name);

        IMutableIndex? userNameIndex = userEntityType.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == "NormalizedUserName"));
        IMutableIndex? emailIndex = userEntityType.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == "NormalizedEmail"));

        Assert.NotNull(userNameIndex);
        Assert.NotNull(emailIndex);
        Assert.True(userNameIndex.IsUnique);
        Assert.False(emailIndex.IsUnique);

        Assert.NotNull(userEntityType.FindProperty("ConcurrencyStamp")?.IsConcurrencyToken);
        Assert.Equal(256, userEntityType.FindProperty("UserName")?.GetMaxLength());
        Assert.Equal(256, userEntityType.FindProperty("NormalizedUserName")?.GetMaxLength());
        Assert.Equal(256, userEntityType.FindProperty("Email")?.GetMaxLength());
        Assert.Equal(256, userEntityType.FindProperty("NormalizedEmail")?.GetMaxLength());
    }

    [Fact]
    public void OnModelCreating_WhenCalled_ConfiguresRoleEntityCorrectly()
    {
        // Arrange
        ModelBuilder modelBuilder = new();

        // Act
        _processor.OnModelCreating(modelBuilder);

        // Assert
        IMutableEntityType? roleEntityType = modelBuilder.Model.FindEntityType(typeof(MyRole));
        Assert.NotNull(roleEntityType);

        IMutableKey? primaryKey = roleEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal("Id", primaryKey.Properties.First().Name);

        IMutableIndex? roleNameIndex = roleEntityType.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == "NormalizedName"));
        Assert.NotNull(roleNameIndex);
        Assert.True(roleNameIndex.IsUnique);

        Assert.NotNull(roleEntityType.FindProperty("ConcurrencyStamp")?.IsConcurrencyToken);
        Assert.Equal(256, roleEntityType.FindProperty("Name")?.GetMaxLength());
        Assert.Equal(256, roleEntityType.FindProperty("NormalizedName")?.GetMaxLength());
    }

    [Fact]
    public void OnModelCreating_WhenCalled_ConfiguresUserClaimEntityCorrectly()
    {
        // Arrange
        ModelBuilder modelBuilder = new();
        // Act
        _processor.OnModelCreating(modelBuilder);

        // Assert
        IMutableEntityType? userClaimEntityType = modelBuilder.Model.FindEntityType(typeof(MyUserClaim));
        Assert.NotNull(userClaimEntityType);

        IMutableKey? primaryKey = userClaimEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal("Id", primaryKey.Properties.First().Name);

        // Assuming your IdentityFeatureProcessor now correctly configures these relationships
        Assert.Contains(userClaimEntityType.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == "UserId") && fk.PrincipalEntityType.ClrType == _feature.UserType);
    }

    [Fact]
    public void OnModelCreating_WhenCalled_ConfiguresUserLoginEntityCorrectly()
    {
        // Arrange
        ModelBuilder modelBuilder = new();

        // Act
        _processor.OnModelCreating(modelBuilder);

        // Assert
        IMutableEntityType? userLoginEntityType = modelBuilder.Model.FindEntityType(typeof(MyUserLogin));
        Assert.NotNull(userLoginEntityType);

        IMutableKey? primaryKey = userLoginEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Contains("LoginProvider", primaryKey.Properties.Select(p => p.Name));
        Assert.Contains("ProviderKey", primaryKey.Properties.Select(p => p.Name));

        // Assuming your IdentityFeatureProcessor now correctly configures these relationships
        Assert.Contains(userLoginEntityType.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == "UserId") && fk.PrincipalEntityType.ClrType == _feature.UserType);
    }

    [Fact]
    public void OnModelCreating_WhenCalled_ConfiguresUserRoleEntityCorrectly()
    {
        // Arrange
        ModelBuilder modelBuilder = new();

        // Act
        _processor.OnModelCreating(modelBuilder);

        // Assert
        IMutableEntityType? userRoleEntityType = modelBuilder.Model.FindEntityType(typeof(MyUserRole));
        Assert.NotNull(userRoleEntityType);

        IMutableKey? primaryKey = userRoleEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Contains("UserId", primaryKey.Properties.Select(p => p.Name));
        Assert.Contains("RoleId", primaryKey.Properties.Select(p => p.Name));

        // Assert: Relationships
        Assert.Contains(userRoleEntityType.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == "UserId") && fk.PrincipalEntityType.ClrType == _feature.UserType);
        Assert.Contains(userRoleEntityType.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == "RoleId") && fk.PrincipalEntityType.ClrType == _feature.RoleType);
    }

    [Fact]
    public void OnModelCreating_WhenCalled_ConfiguresRoleClaimEntityCorrectly()
    {
        // Arrange
        ModelBuilder modelBuilder = new();

        // Act
        _processor.OnModelCreating(modelBuilder);

        // Assert
        IMutableEntityType? roleClaimEntityType = modelBuilder.Model.FindEntityType(typeof(MyRoleClaim));
        Assert.NotNull(roleClaimEntityType);

        IMutableKey? primaryKey = roleClaimEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal("Id", primaryKey.Properties.First().Name);

        // Assert: Relationships
        Assert.Contains(roleClaimEntityType.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == "RoleId") && fk.PrincipalEntityType.ClrType == _feature.RoleType);
    }

    [Fact]
    public void OnModelCreating_WhenCalled_ConfiguresUserTokenEntityCorrectly()
    {
        // Arrange
        ModelBuilder modelBuilder = new();

        // Act
        _processor.OnModelCreating(modelBuilder);

        // Assert
        IMutableEntityType? userTokenEntityType = modelBuilder.Model.FindEntityType(typeof(MyUserToken));
        Assert.NotNull(userTokenEntityType);

        IMutableKey? primaryKey = userTokenEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Contains("UserId", primaryKey.Properties.Select(p => p.Name));
        Assert.Contains("LoginProvider", primaryKey.Properties.Select(p => p.Name));
        Assert.Contains("Name", primaryKey.Properties.Select(p => p.Name));

        // Assert: Relationships
        Assert.Contains(userTokenEntityType.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == "UserId") && fk.PrincipalEntityType.ClrType == _feature.UserType);
    }

    [Fact]
    public void OnModelCreating_WithCustomUserAndRoleTypes_ConfiguresCorrectly()
    {
        // Arrange
        // This test now demonstrates using default IdentityUser<int> and IdentityRole<int>
        // with custom table names via the builder.
        IdentityFeature feature = new DbContextFeaturesBuilder()
            .EnableIdentity<int>(config =>
            {
                config.UsersTableName = "CustomIntUsers";
                config.RolesTableName = "CustomIntRoles";
                config.UserClaimsTableName = "CustomIntUserClaims";
                config.UserRolesTableName = "CustomIntUserRoles";
                config.UserLoginsTableName = "CustomIntUserLogins";
                config.RoleClaimsTableName = "CustomIntRoleClaims";
                config.UserTokensTableName = "CustomIntUserTokens";
            })
            .Build()
            .Identity;

        IdentityFeatureProcessor processor = new(feature, _serviceProvider);
        ModelBuilder modelBuilder = new();

        // Act
        processor.OnModelCreating(modelBuilder);

        // Assert
        IMutableEntityType? userEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUser<int>));
        IMutableEntityType? roleEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityRole<int>));
        IMutableEntityType? userClaimEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserClaim<int>));
        IMutableEntityType? userRoleEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserRole<int>));
        IMutableEntityType? userLoginEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserLogin<int>));
        IMutableEntityType? roleClaimEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityRoleClaim<int>));
        IMutableEntityType? userTokenEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserToken<int>));

        Assert.NotNull(userEntityType);
        Assert.NotNull(roleEntityType);
        Assert.NotNull(userClaimEntityType);
        Assert.NotNull(userRoleEntityType);
        Assert.NotNull(userLoginEntityType);
        Assert.NotNull(roleClaimEntityType);
        Assert.NotNull(userTokenEntityType);

        Assert.Equal("CustomIntUsers", userEntityType.GetTableName());
        Assert.Equal("CustomIntRoles", roleEntityType.GetTableName());
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }

    internal class IdentityFeatureProcessorWithDbSetTests_DbContext : DbContext
    {
        public IdentityFeatureProcessorWithDbSetTests_DbContext(DbContextOptions<IdentityFeatureProcessorWithDbSetTests_DbContext> options) : base(options) { }

        public DbSet<MyUser> Users { get; set; } = default!;
        public DbSet<MyRole> Roles { get; set; } = default!;
        public DbSet<MyUserClaim> UserClaims { get; set; } = default!;
        public DbSet<MyUserLogin> UserLogins { get; set; } = default!;
        public DbSet<MyUserRole> UserRoles { get; set; } = default!;
        public DbSet<MyRoleClaim> RoleClaims { get; set; } = default!;
        public DbSet<MyUserToken> UserTokens { get; set; } = default!;

    }
}
