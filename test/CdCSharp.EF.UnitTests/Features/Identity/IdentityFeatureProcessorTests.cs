using CdCSharp.EF.Features.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.UnitTests.Features.Identity;

public class IdentityFeatureProcessorTests : IDisposable
{
    private readonly IdentityFeatureProcessorTests_DbContext _context;
    private readonly IdentityFeatureProcessor _processor;
    private readonly ServiceProvider _serviceProvider;
    private readonly IdentityFeature _feature;

    public IdentityFeatureProcessorTests()
    {
        ServiceCollection services = new();
        _serviceProvider = services.BuildServiceProvider();

        DbContextOptions<IdentityFeatureProcessorTests_DbContext> options = new DbContextOptionsBuilder<IdentityFeatureProcessorTests_DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new IdentityFeatureProcessorTests_DbContext(options);

        _feature = new() // Initialize _feature here
        {
            Enabled = true,
            UserType = typeof(IdentityUser<Guid>),
            RoleType = typeof(IdentityRole<Guid>),
            UserClaimType = typeof(IdentityUserClaim<Guid>),
            UserRoleType = typeof(IdentityUserRole<Guid>),
            UserLoginType = typeof(IdentityUserLogin<Guid>),
            RoleClaimType = typeof(IdentityRoleClaim<Guid>),
            UserTokenType = typeof(IdentityUserToken<Guid>),
            Configuration = new IdentityConfiguration()
        };

        _processor = new IdentityFeatureProcessor(_feature, _serviceProvider);
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange
        IdentityFeature feature = new();

        // Act
        IdentityFeatureProcessor processor = new(feature, _serviceProvider);

        // Assert
        Assert.NotNull(processor);
    }

    [Fact]
    public void OnModelCreating_WhenCalled_ConfiguresAllIdentityEntities()
    {
        // Arrange
        ModelBuilder modelBuilder = new();

        // Act
        _processor.OnModelCreating(modelBuilder);

        // Assert
        // Verificar que se han configurado las entidades principales
        IMutableEntityType? userEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUser<Guid>));
        IMutableEntityType? roleEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityRole<Guid>));
        IMutableEntityType? userClaimEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserClaim<Guid>));
        IMutableEntityType? userRoleEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserRole<Guid>));
        IMutableEntityType? userLoginEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserLogin<Guid>));
        IMutableEntityType? roleClaimEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityRoleClaim<Guid>));
        IMutableEntityType? userTokenEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserToken<Guid>));

        Assert.NotNull(userEntityType);
        Assert.NotNull(roleEntityType);
        Assert.NotNull(userClaimEntityType);
        Assert.NotNull(userRoleEntityType);
        Assert.NotNull(userLoginEntityType);
        Assert.NotNull(roleClaimEntityType);
        Assert.NotNull(userTokenEntityType);

        // Verificar nombres de tabla por defecto
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
        IdentityConfiguration customConfig = new()
        {
            UsersTableName = "CustomUsers",
            RolesTableName = "CustomRoles",
            UserClaimsTableName = "CustomUserClaims",
            UserRolesTableName = "CustomUserRoles",
            UserLoginsTableName = "CustomUserLogins",
            RoleClaimsTableName = "CustomRoleClaims",
            UserTokensTableName = "CustomUserTokens"
        };

        IdentityFeature feature = new()
        {
            Enabled = true,
            UserType = typeof(IdentityUser<Guid>), // Ensure types are set for custom tables
            RoleType = typeof(IdentityRole<Guid>),
            UserClaimType = typeof(IdentityUserClaim<Guid>),
            UserRoleType = typeof(IdentityUserRole<Guid>),
            UserLoginType = typeof(IdentityUserLogin<Guid>),
            RoleClaimType = typeof(IdentityRoleClaim<Guid>),
            UserTokenType = typeof(IdentityUserToken<Guid>),
            Configuration = customConfig
        };

        IdentityFeatureProcessor processor = new(feature, _serviceProvider);
        ModelBuilder modelBuilder = new();

        // Act
        processor.OnModelCreating(modelBuilder);

        // Assert
        IMutableEntityType? userEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUser<Guid>));
        IMutableEntityType? roleEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityRole<Guid>));
        IMutableEntityType? userClaimEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserClaim<Guid>));
        IMutableEntityType? userRoleEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserRole<Guid>));
        IMutableEntityType? userLoginEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserLogin<Guid>));
        IMutableEntityType? roleClaimEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityRoleClaim<Guid>));
        IMutableEntityType? userTokenEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserToken<Guid>));

        Assert.Equal("CustomUsers", userEntityType?.GetTableName());
        Assert.Equal("CustomRoles", roleEntityType?.GetTableName());
        Assert.Equal("CustomUserClaims", userClaimEntityType?.GetTableName());
        Assert.Equal("CustomUserRoles", userRoleEntityType?.GetTableName());
        Assert.Equal("CustomUserLogins", userLoginEntityType?.GetTableName());
        Assert.Equal("CustomRoleClaims", roleClaimEntityType?.GetTableName());
        Assert.Equal("CustomUserTokens", userTokenEntityType?.GetTableName());
    }

    [Fact]
    public void OnModelCreatingEntity_WhenCalled_DoesNothing()
    {
        // Arrange
        ModelBuilder modelBuilder = new();
        Type entityType = typeof(IdentityUser<Guid>);

        // Act & Assert - Should not throw
        _processor.OnModelCreatingEntity(modelBuilder, entityType);
    }

    [Fact]
    public void OnSaveChanges_WhenCalled_DoesNothing() =>
        // Arrange & Act & Assert - Should not throw
        _processor.OnSaveChanges(_context.ChangeTracker);

    [Fact]
    public void OnModelCreating_WhenCalled_ConfiguresUserEntityCorrectly()
    {
        // Arrange
        ModelBuilder modelBuilder = new();

        // Act
        _processor.OnModelCreating(modelBuilder);

        // Assert
        IMutableEntityType? userEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUser<Guid>));
        Assert.NotNull(userEntityType);

        // Verificar que tiene clave primaria
        IMutableKey? primaryKey = userEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal("Id", primaryKey.Properties.First().Name);

        // Verificar índices
        IMutableIndex? userNameIndex = userEntityType.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == "NormalizedUserName"));
        IMutableIndex? emailIndex = userEntityType.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == "NormalizedEmail"));

        Assert.NotNull(userNameIndex);
        Assert.NotNull(emailIndex);
        Assert.True(userNameIndex.IsUnique);
        Assert.False(emailIndex.IsUnique);

        // Verificar propiedades
        Assert.NotNull(userEntityType.FindProperty("ConcurrencyStamp")?.IsConcurrencyToken);
        Assert.Equal(256, userEntityType.FindProperty("UserName")?.GetMaxLength());
        Assert.Equal(256, userEntityType.FindProperty("NormalizedUserName")?.GetMaxLength());
        Assert.Equal(256, userEntityType.FindProperty("Email")?.GetMaxLength());
        Assert.Equal(256, userEntityType.FindProperty("NormalizedEmail")?.GetMaxLength());

        // Verify foreign keys from UserClaim, UserLogin, UserToken, UserRole point to User
        IMutableEntityType? userClaimEntityType = modelBuilder.Model.FindEntityType(_feature.UserClaimType);
        Assert.NotNull(userClaimEntityType);
        Assert.Contains(userClaimEntityType.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == "UserId") && fk.PrincipalEntityType == userEntityType);

        IMutableEntityType? userLoginEntityType = modelBuilder.Model.FindEntityType(_feature.UserLoginType);
        Assert.NotNull(userLoginEntityType);
        Assert.Contains(userLoginEntityType.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == "UserId") && fk.PrincipalEntityType == userEntityType);

        IMutableEntityType? userTokenType = modelBuilder.Model.FindEntityType(_feature.UserTokenType);
        Assert.NotNull(userTokenType);
        Assert.Contains(userTokenType.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == "UserId") && fk.PrincipalEntityType == userEntityType);

        IMutableEntityType? userRoleEntityType = modelBuilder.Model.FindEntityType(_feature.UserRoleType);
        Assert.NotNull(userRoleEntityType);
        Assert.Contains(userRoleEntityType.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == "UserId") && fk.PrincipalEntityType == userEntityType);
    }

    [Fact]
    public void OnModelCreating_WhenCalled_ConfiguresRoleEntityCorrectly()
    {
        // Arrange
        ModelBuilder modelBuilder = new();

        // Act
        _processor.OnModelCreating(modelBuilder);

        // Assert
        IMutableEntityType? roleEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityRole<Guid>));
        Assert.NotNull(roleEntityType);

        // Verificar que tiene clave primaria
        IMutableKey? primaryKey = roleEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal("Id", primaryKey.Properties.First().Name);

        // Verificar índice de nombre de rol
        IMutableIndex? roleNameIndex = roleEntityType.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == "NormalizedName"));
        Assert.NotNull(roleNameIndex);
        Assert.True(roleNameIndex.IsUnique);

        // Verificar propiedades
        Assert.NotNull(roleEntityType.FindProperty("ConcurrencyStamp")?.IsConcurrencyToken);
        Assert.Equal(256, roleEntityType.FindProperty("Name")?.GetMaxLength());
        Assert.Equal(256, roleEntityType.FindProperty("NormalizedName")?.GetMaxLength());

        // Verificar que los Foreign Keys de UserRole y RoleClaim apuntan a Role
        IMutableEntityType? userRoleEntityType = modelBuilder.Model.FindEntityType(_feature.UserRoleType);
        Assert.NotNull(userRoleEntityType);
        Assert.Contains(userRoleEntityType.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == "RoleId") && fk.PrincipalEntityType == roleEntityType);

        IMutableEntityType? roleClaimEntityType = modelBuilder.Model.FindEntityType(_feature.RoleClaimType);
        Assert.NotNull(roleClaimEntityType);
        Assert.Contains(roleClaimEntityType.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == "RoleId") && fk.PrincipalEntityType == roleEntityType);
    }

    [Fact]
    public void OnModelCreating_WhenCalled_ConfiguresUserClaimEntityCorrectly()
    {
        // Arrange
        ModelBuilder modelBuilder = new();

        // Act
        _processor.OnModelCreating(modelBuilder);

        // Assert
        IMutableEntityType? userClaimEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserClaim<Guid>));
        Assert.NotNull(userClaimEntityType);

        // Verify primary key
        IMutableKey? primaryKey = userClaimEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal("Id", primaryKey.Properties.First().Name);

        // Verify foreign key to User
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
        IMutableEntityType? userLoginEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserLogin<Guid>));
        Assert.NotNull(userLoginEntityType);

        // Verify composite primary key
        IMutableKey? primaryKey = userLoginEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Contains("LoginProvider", primaryKey.Properties.Select(p => p.Name));
        Assert.Contains("ProviderKey", primaryKey.Properties.Select(p => p.Name));

        // Verify foreign key to User
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
        IMutableEntityType? userRoleEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserRole<Guid>));
        Assert.NotNull(userRoleEntityType);

        // Verify composite primary key
        IMutableKey? primaryKey = userRoleEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Contains("UserId", primaryKey.Properties.Select(p => p.Name));
        Assert.Contains("RoleId", primaryKey.Properties.Select(p => p.Name));

        // Verify foreign keys to User and Role
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
        IMutableEntityType? roleClaimEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityRoleClaim<Guid>));
        Assert.NotNull(roleClaimEntityType);

        // Verify primary key
        IMutableKey? primaryKey = roleClaimEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal("Id", primaryKey.Properties.First().Name);

        // Verify foreign key to Role
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
        IMutableEntityType? userTokenEntityType = modelBuilder.Model.FindEntityType(typeof(IdentityUserToken<Guid>));
        Assert.NotNull(userTokenEntityType);

        // Verify composite primary key
        IMutableKey? primaryKey = userTokenEntityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Contains("UserId", primaryKey.Properties.Select(p => p.Name));
        Assert.Contains("LoginProvider", primaryKey.Properties.Select(p => p.Name));
        Assert.Contains("Name", primaryKey.Properties.Select(p => p.Name));

        // Verify foreign key to User
        Assert.Contains(userTokenEntityType.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == "UserId") && fk.PrincipalEntityType.ClrType == _feature.UserType);
    }

    [Fact]
    public void OnModelCreating_WithCustomUserAndRoleTypes_ConfiguresCorrectly()
    {
        // Arrange
        IdentityFeature feature = new()
        {
            Enabled = true,
            UserType = typeof(IdentityUser<int>),
            RoleType = typeof(IdentityRole<int>),
            UserClaimType = typeof(IdentityUserClaim<int>),
            UserRoleType = typeof(IdentityUserRole<int>),
            UserLoginType = typeof(IdentityUserLogin<int>),
            RoleClaimType = typeof(IdentityRoleClaim<int>),
            UserTokenType = typeof(IdentityUserToken<int>),
            Configuration = new IdentityConfiguration()
        };

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

        // Basic check for table names with custom types (they should still default if not overridden in config)
        Assert.Equal("AspNetUsers", userEntityType.GetTableName());
        Assert.Equal("AspNetRoles", roleEntityType.GetTableName());
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }

    internal class IdentityFeatureProcessorTests_DbContext : DbContext
    {
        public IdentityFeatureProcessorTests_DbContext(DbContextOptions<IdentityFeatureProcessorTests_DbContext> options) : base(options) { }
    }
}