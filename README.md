# CdCSharp.EF

[![NuGet](https://img.shields.io/nuget/v/CdCSharp.EF.svg)](https://www.nuget.org/packages/CdCSharp.EF)
[![License](https://img.shields.io/github/license/smaicas/CdCSharp.EF)](LICENSE)
[![Build Status](https://img.shields.io/github/actions/workflow/status/smaicas/CdCSharp.EF/dotnet.yml?branch=master)](https://github.com/smaicas/CdCSharp.EF/actions/workflows/dotnet.yml)
[![GitHub Stars](https://img.shields.io/github/stars/smaicas/CdCSharp.EF)](https://github.com/smaicas/CdCSharp.EF/stargazers)

An extensible Entity Framework Core library that provides common enterprise application features like automatic auditing, multi-tenancy, and ASP.NET Core Identity integration.

## ✨ Key Features

- 🔍 **Automatic Auditing**: Automatic audit fields (CreatedDate, LastModifiedDate, CreatedBy, ModifiedBy)
- 🏢 **Multi-Tenancy**: Support for multi-tenant applications with separate database or discriminator strategies
- 👤 **Identity Integration**: Automatic ASP.NET Core Identity configuration
- 🔧 **Extensible**: Customizable feature processor-based architecture
- ⚡ **Easy Configuration**: Fluent API for quick setup
- 🌐 **ASP.NET Core Ready**: Integrated middleware for web applications

## 🚀 Installation

```bash
dotnet add package CdCSharp.EF
```

## 📋 Table of Contents

- [Quick Start](#-quick-start)
- [Features](#-features)
  - [Auditing](#auditing)
  - [Multi-Tenancy](#multi-tenancy)
  - [Identity](#identity)
- [Advanced Configuration](#-advanced-configuration)
- [Extensibility](#-extensibility)
- [Examples](#-examples)
- [API Reference](#-api-reference)
- [Architecture](#-architecture)
- [Contributing](#-contributing)
- [License](#-license)

## 🏁 Quick Start

### 1. Basic Configuration

```csharp
// Program.cs
using CdCSharp.EF.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure DbContext with features
builder.Services.AddExtensibleDbContext<MyDbContext>(
    options => options.UseSqlServer(connectionString),
    features => features
        .EnableAuditing()
        .EnableMultiTenantByDiscriminator()
        .EnableIdentity<Guid>()
);

var app = builder.Build();

// Configure middleware
app.UseCurrentUser();
app.UseMultiTenant();
```

### 2. Define your DbContext

```csharp
public class MyDbContext : ExtensibleDbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options, IServiceProvider serviceProvider) 
        : base(options, serviceProvider) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
}
```

### 3. Define Entities

```csharp
// Entity with auditing
public class Product : IAuditableWithUserEntity, ITenantEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    
    // Audit fields (automatically populated)
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    
    // Multi-tenancy
    public string TenantId { get; set; }
}
```

## 🎯 Features

### Auditing

The auditing feature provides automatic change tracking for entities.

#### Configuration

```csharp
services.AddExtensibleDbContext<MyDbContext>(
    options => options.UseSqlServer(connectionString),
    features => features.EnableAuditing(config =>
    {
        config.BehaviorWhenNoUser = AuditingBehavior.UseDefaultUser;
        config.DefaultUserId = "SYSTEM";
    })
);
```

#### Entity Interfaces

```csharp
// Basic auditing (dates only)
public class BasicEntity : IAuditableEntity
{
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
}

// Auditing with user tracking
public class FullEntity : IAuditableWithUserEntity
{
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}
```

#### Auditing Behaviors

```csharp
public enum AuditingBehavior
{
    ThrowException,    // Throw exception if no user available
    UseDefaultUser,    // Use a default user
    SaveAsNull,        // Save null in user fields
    SkipUserFields     // Don't modify user fields
}
```

### Multi-Tenancy

Support for multi-tenant applications with two main strategies.

#### Discriminator Strategy

All tenants share the same database, filtered by `TenantId`.

```csharp
services.AddExtensibleDbContext<MyDbContext>(
    options => options.UseSqlServer(connectionString),
    features => features.EnableMultiTenantByDiscriminator()
);

// Multi-tenant entity
public class Product : ITenantEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string TenantId { get; set; } // Automatically populated
}
```

#### Database Strategy

Each tenant has its own database.

```csharp
services.AddExtensibleDbContext<MyDbContext>(
    features => features.EnableMultiTenantByDatabase(tenants => tenants
        .AddTenant("tenant1", options => 
            options.UseSqlServer("Server=...;Database=Tenant1"))
        .AddTenant("tenant2", options => 
            options.UseSqlServer("Server=...;Database=Tenant2"))
    )
);
```

#### Tenant Resolution

```csharp
// Default: HTTP Header "X-Tenant-Id"
app.UseMultiTenant();

// Custom resolver
services.AddCustomTenantResolver<MyTenantResolver>();

public class MyTenantResolver : ITenantResolver
{
    public Task<string?> ResolveTenantIdAsync()
    {
        // Your custom logic
        return Task.FromResult("tenant1");
    }
}
```

### Identity

Automatic integration with ASP.NET Core Identity.

#### Basic Configuration

```csharp
services.AddExtensibleDbContext<MyDbContext>(
    options => options.UseSqlServer(connectionString),
    features => features.EnableIdentity<Guid>()
);
```

#### Advanced Configuration

```csharp
services.AddExtensibleDbContext<MyDbContext>(
    options => options.UseSqlServer(connectionString),
    features => features.EnableIdentity<Guid>(config =>
    {
        config.UsersTableName = "MyUsers";
        config.RolesTableName = "MyRoles";
        config.Options.Password.RequiredLength = 8;
    })
);
```

#### Identity with Custom Types

```csharp
// Define custom types
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class ApplicationRole : IdentityRole<Guid>
{
    public string Description { get; set; }
}

// Configure
services.AddExtensibleDbContext<MyDbContext>(
    options => options.UseSqlServer(connectionString),
    features => features.EnableIdentity<Guid, ApplicationUser, ApplicationRole>()
);
```

## ⚙️ Advanced Configuration

### Combining Features

```csharp
services.AddExtensibleDbContext<MyDbContext>(
    options => options.UseSqlServer(connectionString),
    features => features
        .EnableAuditing(config =>
        {
            config.BehaviorWhenNoUser = AuditingBehavior.UseDefaultUser;
            config.DefaultUserId = "SYSTEM";
        })
        .EnableMultiTenantByDiscriminator()
        .EnableIdentity<Guid>(config =>
        {
            config.Options.Password.RequiredLength = 8;
            config.Options.Password.RequireDigit = true;
        })
);
```

### Custom Resolvers

```csharp
// Claims-based user resolver
public class ClaimsCurrentUserResolver : ICurrentUserResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public ClaimsCurrentUserResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public Task<string?> ResolveCurrentUserIdAsync()
    {
        var userId = _httpContextAccessor.HttpContext?.User
            ?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Task.FromResult(userId);
    }
}

// Register custom resolver
services.AddCustomCurrentUserResolver<ClaimsCurrentUserResolver>();
```

### Custom Stores

```csharp
public class DatabaseTenantStore : ITenantStore
{
    private readonly MyDbContext _context;
    
    public DatabaseTenantStore(MyDbContext context)
    {
        _context = context;
    }
    
    public string? GetCurrentTenantId()
    {
        // Logic to get tenant from database
        return "tenant1";
    }
}

services.AddCustomTenantStore<DatabaseTenantStore>();
```

## 🔧 Extensibility

### Custom Feature Processors

```csharp
public class CustomFeatureProcessor : IFeatureProcessor
{
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Model configuration
    }
    
    public void OnModelCreatingEntity(ModelBuilder modelBuilder, Type entityType, ExtensibleDbContext context)
    {
        // Per-entity configuration
    }
    
    public void OnSaveChanges(ChangeTracker changeTracker)
    {
        // Logic before saving
    }
}

// Register custom processor
services.AddScoped<IFeatureProcessor, CustomFeatureProcessor>();
```

### Extension Interfaces

```csharp
// For auditable entities
public interface IAuditableEntity
{
    DateTime CreatedDate { get; set; }
    DateTime LastModifiedDate { get; set; }
}

// For multi-tenant entities
public interface ITenantEntity
{
    string TenantId { get; set; }
}

// For user resolution
public interface ICurrentUserResolver
{
    Task<string?> ResolveCurrentUserIdAsync();
}

// For tenant resolution
public interface ITenantResolver
{
    Task<string?> ResolveTenantIdAsync();
}
```

## 📚 Examples

### Example 1: Multi-Tenant API with Auditing

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly MyDbContext _context;
    
    public ProductsController(MyDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        // Automatically filtered by tenant
        var products = await _context.Products.ToListAsync();
        return Ok(products);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateProduct(ProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price
            // TenantId, CreatedDate, CreatedBy are filled automatically
        };
        
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }
}
```

### Example 2: Complex Configuration

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        
        services.AddExtensibleDbContext<ApplicationDbContext>(
            options => options.UseSqlServer(connectionString),
            features => features
                .EnableAuditing(audit =>
                {
                    audit.BehaviorWhenNoUser = AuditingBehavior.UseDefaultUser;
                    audit.DefaultUserId = "SYSTEM";
                })
                .EnableMultiTenantByDatabase(tenants => tenants
                    .AddTenant("company-a", opts => 
                        opts.UseSqlServer("Server=...;Database=CompanyA"))
                    .AddTenant("company-b", opts => 
                        opts.UseSqlServer("Server=...;Database=CompanyB"))
                )
                .EnableIdentity<Guid, ApplicationUser, ApplicationRole>(identity =>
                {
                    identity.UsersTableName = "Users";
                    identity.RolesTableName = "Roles";
                    identity.Options.Password.RequiredLength = 10;
                })
        );
        
        // Custom resolvers
        services.AddCustomTenantResolver<SubdomainTenantResolver>();
        services.AddCustomCurrentUserResolver<JwtUserResolver>();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseAuthentication();
        app.UseCurrentUser();
        app.UseMultiTenant();
        app.UseAuthorization();
    }
}
```

### Example 3: Query String Tenant Resolver

```csharp
public class QueryStringTenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public QueryStringTenantResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public Task<string?> ResolveTenantIdAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Request.Query.TryGetValue("tenant", out var tenantValues) == true)
        {
            return Task.FromResult(tenantValues.FirstOrDefault());
        }
        
        return Task.FromResult<string?>(null);
    }
}

// Usage: /api/products?tenant=company-a
services.AddCustomTenantResolver<QueryStringTenantResolver>();
```

### Example 4: JWT-based User Resolver

```csharp
public class JwtUserResolver : ICurrentUserResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public JwtUserResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public Task<string?> ResolveCurrentUserIdAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirst("sub")?.Value ?? 
                     user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        return Task.FromResult(userId);
    }
}
```

## 📖 API Reference

### Main Extension Methods

```csharp
// DbContext configuration
services.AddExtensibleDbContext<TContext>(
    Action<DbContextOptionsBuilder>? configureOptions = null,
    Func<DbContextFeaturesBuilder, DbContextFeaturesBuilder>? featuresBuilder = null)

// Features
DbContextFeaturesBuilder.EnableAuditing(Action<AuditingConfiguration>? configure = null)
DbContextFeaturesBuilder.EnableIdentity<TKey>(Action<IdentityConfiguration>? configure = null)
DbContextFeaturesBuilder.EnableMultiTenantByDiscriminator(Action<DbContextOptionsBuilder>? options = null)
DbContextFeaturesBuilder.EnableMultiTenantByDatabase(Action<MultiTenantByDatabaseBuilder> buildTenants)

// Custom resolvers and stores
services.AddCustomTenantResolver<TResolver>()
services.AddCustomTenantStore<TStore>()
services.AddCustomCurrentUserResolver<TResolver>()
services.AddCustomCurrentUserStore<TStore>()

// Middleware
app.UseMultiTenant()
app.UseCurrentUser()
```

### Available Configurations

```csharp
// AuditingConfiguration
public class AuditingConfiguration
{
    public AuditingBehavior BehaviorWhenNoUser { get; set; }
    public string? DefaultUserId { get; set; }
}

// IdentityConfiguration
public class IdentityConfiguration
{
    public IdentityOptions Options { get; set; }
    public string UsersTableName { get; set; }
    public string RolesTableName { get; set; }
    public string UserClaimsTableName { get; set; }
    public string UserRolesTableName { get; set; }
    public string UserLoginsTableName { get; set; }
    public string RoleClaimsTableName { get; set; }
    public string UserTokensTableName { get; set; }
}

// MultiTenantConfiguration
public class MultiTenantConfiguration
{
    public MultiTenantStrategy Strategy { get; set; }
    public Dictionary<string, Action<DbContextOptionsBuilder>> DatabaseConfigurations { get; set; }
    public Action<DbContextOptionsBuilder>? DiscriminatorConfiguration { get; set; }
}
```

### Core Interfaces

```csharp
// Entity interfaces
public interface IAuditableEntity
{
    DateTime CreatedDate { get; set; }
    DateTime LastModifiedDate { get; set; }
}

public interface IAuditableWithUserEntity : IAuditableEntity
{
    string? CreatedBy { get; set; }
    string? ModifiedBy { get; set; }
}

public interface ITenantEntity
{
    string TenantId { get; set; }
}

// Resolution interfaces
public interface ICurrentUserResolver
{
    Task<string?> ResolveCurrentUserIdAsync();
}

public interface ITenantResolver
{
    Task<string?> ResolveTenantIdAsync();
}

// Store interfaces
public interface ICurrentUserStore
{
    string? GetCurrentUserId();
}

public interface IWritableCurrentUserStore : ICurrentUserStore
{
    void SetCurrentUserId(string userId);
    void ClearCurrentUserId();
}

public interface ITenantStore
{
    string? GetCurrentTenantId();
}

public interface IWritableTenantStore : ITenantStore
{
    void SetCurrentTenantId(string tenantId);
    void ClearCurrentTenantId();
}

// Extensibility interfaces
public interface IFeatureProcessor
{
    void OnModelCreating(ModelBuilder modelBuilder);
    void OnModelCreatingEntity(ModelBuilder modelBuilder, Type entityType, ExtensibleDbContext context);
    void OnSaveChanges(ChangeTracker changeTracker);
}
```

## 🏗️ Architecture

The library is built around a feature processor architecture that allows for clean separation of concerns and easy extensibility:

- **ExtensibleDbContext**: Base DbContext that orchestrates feature processors
- **Feature Processors**: Handle specific functionality (auditing, multi-tenancy, identity)
- **Resolvers**: Determine current user/tenant from request context
- **Stores**: Maintain current user/tenant state during request lifetime
- **Middleware**: Integrates with ASP.NET Core pipeline

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built on top of Entity Framework Core
- Inspired by common enterprise application patterns
- Thanks to all contributors and users of this library