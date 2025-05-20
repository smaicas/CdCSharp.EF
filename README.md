# CdCSharp.EF - Multi-Tenant & Features Library

[![NuGet](https://img.shields.io/nuget/v/CdCSharp.EF.svg)](https://www.nuget.org/packages/CdCSharp.EF)
[![License](https://img.shields.io/github/license/smaicas/CdCSharp.EF)](LICENSE)
[![Build Status](https://img.shields.io/github/actions/workflow/status/smaicas/CdCSharp.EF/dotnet.yml?branch=master)](https://github.com/smaicas/CdCSharp.EF/actions/workflows/dotnet.yml)

A complete and extensible library for implementing **Multi-Tenancy** and **automatic features** (like auditing) in .NET 9 applications with Entity Framework Core, offering transparent data isolation, automatic auditing, and flexible configuration.

## 📚 Table of Contents

- [Features](#-features)
- [Installation](#-installation)
- [Quick Setup](#-quick-setup)
- [Multi-Tenancy Strategies](#-multi-tenancy-strategies)
- [Automatic Features](#-automatic-features)
- [Basic Usage](#-basic-usage)
- [Advanced Customization](#-advanced-customization)
- [API Reference](#-api-reference)
- [Best Practices](#-best-practices)
- [Testing](#-testing)
- [Contributing](#-contributing)

## ✨ Features

### Multi-Tenancy
- ✅ **Two Multi-Tenancy strategies**: By Discriminator (single DB) and By Database (multiple DBs)
- ✅ **Automatic filtering**: Automatic query filters per tenant with relationship support
- ✅ **Flexible resolution**: Extensible system to resolve current tenant (Headers, Claims, Query String, etc.)
- ✅ **Configurable stores**: Tenant storage with in-memory and claims-based implementations
- ✅ **Smart middleware**: Only activates when necessary (writable stores)
- ✅ **Automatic injection**: Automatic TenantId establishment in entities

### Automatic Features System
- ✅ **Automatic Auditing**: Configurable audit fields with multiple behaviors
- ✅ **Current User tracking**: Automatic user resolution and injection
- ✅ **Extensible features**: Plugin system for custom automatic behaviors
- ✅ **Flexible configuration**: Multiple audit behaviors when user is not available
- ✅ **Entity interfaces**: Optional auditing per entity type

### Development Experience
- ✅ **Fluent APIs**: Simple and clear configuration with extension methods
- ✅ **Type-safe**: Separate interfaces for read-only vs read/write stores
- ✅ **Builder patterns**: Clean configuration for complex scenarios
- ✅ **Fully testable**: Complete suite of unit and integration tests

## 📦 Installation

```bash
# Via Package Manager
Install-Package CdCSharp.EF

# Via .NET CLI
dotnet add package CdCSharp.EF

# Via PackageReference
<PackageReference Include="CdCSharp.EF" Version="1.0.0" />
```

## 🚀 Quick Setup

### 1. Define your entities

```csharp
using CdCSharp.EF.Core.Abstractions;

// Multi-tenant entity
public class Product : ITenantEntity, IAuditableWithUserEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    // ITenantEntity
    public string TenantId { get; set; } = string.Empty;
    
    // IAuditableWithUserEntity (automatic auditing)
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

// Entity with basic auditing (no user tracking)
public class Order : ITenantEntity, IAuditableEntity
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    
    // ITenantEntity
    public string TenantId { get; set; } = string.Empty;
    
    // IAuditableEntity
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
}

// Entity without auditing
public class OrderItem : ITenantEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string TenantId { get; set; } = string.Empty;
}
```

### 2. Create your DbContext

```csharp
using CdCSharp.EF.Core;
using Microsoft.EntityFrameworkCore;

// For multi-tenant applications
public class ApplicationDbContext : MultiTenantDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
}

// For single-tenant applications with features
public class SimpleAppDbContext : ExtensibleDbContext
{
    public SimpleAppDbContext(DbContextOptions<SimpleAppDbContext> options, IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<User> Users { get; set; }
}
```

### 3. Configure services in Program.cs

```csharp
using CdCSharp.EF.Extensions;
using CdCSharp.EF.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Option 1: Multi-tenant with features (discriminator strategy)
builder.Services.AddMultiTenantByDiscriminatorDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")),
    features => features.EnableAuditing(audit => 
    {
        audit.BehaviorWhenNoUser = AuditingBehavior.UseDefaultUser;
        audit.DefaultUserId = "SYSTEM";
    })
);

// Option 2: Multi-tenant with separate databases
builder.Services.AddMultiTenantByDatabaseDbContext<ApplicationDbContext>(
    tenants => tenants
        .AddTenant("tenant1", options => 
            options.UseSqlServer(builder.Configuration.GetConnectionString("Tenant1")))
        .AddTenant("tenant2", options => 
            options.UseSqlServer(builder.Configuration.GetConnectionString("Tenant2")))
        .AddTenant("tenant3", options => 
            options.UseSqlServer(builder.Configuration.GetConnectionString("Tenant3"))),
    features => features.EnableAuditing()
);

// Option 3: Simple application with auditing (no multi-tenancy)
builder.Services.AddExtensibleDbContext<SimpleAppDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")),
    features => features.EnableAuditing(audit => 
    {
        audit.BehaviorWhenNoUser = AuditingBehavior.ThrowException;
    })
);

builder.Services.AddControllers();

var app = builder.Build();

// Register middleware (automatically activates only when necessary)
app.UseCurrentUser(); // For user tracking
app.UseMultiTenant(); // For multi-tenancy
app.UseRouting();
app.MapControllers();

app.Run();
```

### 4. Use in controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
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
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
    {
        var product = new Product 
        { 
            Name = dto.Name, 
            Price = dto.Price 
            // TenantId, CreatedDate, CreatedBy, etc. are set automatically
        };
        
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        
        product.Name = dto.Name;
        product.Price = dto.Price;
        // LastModifiedDate and ModifiedBy are set automatically
        
        await _context.SaveChangesAsync();
        return Ok(product);
    }
}
```

### 5. Use with HTTP headers

```bash
# Multi-tenant requests
curl -H "X-Tenant-Id: tenant1" -H "X-User-Id: user123" https://api.example.com/api/products

# Or with JWT (if using ClaimsCurrentUserResolver)
curl -H "Authorization: Bearer <jwt-with-tenant-and-user-claims>" https://api.example.com/api/products
```

## 🏗️ Multi-Tenancy Strategies

### Discriminator Strategy

A single database with automatic filtering by `TenantId`. **Recommended** for most use cases.

```csharp
// Simple configuration
builder.Services.AddMultiTenantByDiscriminatorDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString),
    features => features.EnableAuditing()
);
```

**✅ Advantages:**
- Single database to maintain
- More resource efficient
- Simplified backup and maintenance
- Simpler migrations

**❌ Disadvantages:**
- All tenants share the same database
- Less physical data isolation
- Potential performance issues with high data volume

### Database Strategy

A separate database per tenant. **Recommended** for large tenants or when complete isolation is required.

```csharp
// Multiple configuration with builder pattern
builder.Services.AddMultiTenantByDatabaseDbContext<ApplicationDbContext>(
    tenants => tenants
        .AddTenant("tenant1", options => options.UseSqlServer(tenant1ConnectionString))
        .AddTenant("tenant2", options => options.UseSqlServer(tenant2ConnectionString))
        .AddTenant("tenant3", options => options.UseSqlServer(tenant3ConnectionString)),
    features => features.EnableAuditing()
);
```

**✅ Advantages:**
- Complete data isolation
- Better performance per tenant
- Independent backups
- Horizontal scalability

**❌ Disadvantages:**
- Greater maintenance complexity
- More resources required
- More complex migrations

## 🎯 Automatic Features

### Auditing Feature

Automatic tracking of creation and modification dates and users.

#### Entity Interfaces

```csharp
// Basic auditing (dates only)
public interface IAuditableEntity
{
    DateTime CreatedDate { get; set; }
    DateTime LastModifiedDate { get; set; }
}

// Full auditing (dates + users)
public interface IAuditableWithUserEntity : IAuditableEntity
{
    string? CreatedBy { get; set; }
    string? ModifiedBy { get; set; }
}
```

#### Auditing Configuration

```csharp
builder.Services.AddMultiTenantByDiscriminatorDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString),
    features => features.EnableAuditing(audit => 
    {
        // Behavior when no current user is available
        audit.BehaviorWhenNoUser = AuditingBehavior.UseDefaultUser;
        audit.DefaultUserId = "SYSTEM";
    })
);
```

#### Available Auditing Behaviors

```csharp
public enum AuditingBehavior
{
    ThrowException,  // Throws exception if can't resolve user
    UseDefaultUser,  // Uses configured default user
    SaveAsNull,      // Saves null in user fields (default)
    SkipUserFields   // Leaves user fields unchanged
}
```

### Auditing Configuration Examples

```csharp
// Throw exception when no user
features => features.EnableAuditing(audit => 
{
    audit.BehaviorWhenNoUser = AuditingBehavior.ThrowException;
})

// Use system user when no user available
features => features.EnableAuditing(audit => 
{
    audit.BehaviorWhenNoUser = AuditingBehavior.UseDefaultUser;
    audit.DefaultUserId = "SYSTEM";
})

// Save null when no user (default behavior)
features => features.EnableAuditing(audit => 
{
    audit.BehaviorWhenNoUser = AuditingBehavior.SaveAsNull;
})

// Don't modify user fields when no user
features => features.EnableAuditing(audit => 
{
    audit.BehaviorWhenNoUser = AuditingBehavior.SkipUserFields;
})

// Quick configurations
features => features.EnableAuditing() // Uses default config (SaveAsNull)
// Or use predefined configurations
var auditConfig = AuditingConfiguration.ThrowOnMissingUser;
var auditConfig = AuditingConfiguration.UseSystemUser("SYSTEM");
var auditConfig = AuditingConfiguration.SkipUserFields;
```

## 💡 Basic Usage

### Simple Application (No Multi-Tenancy)

```csharp
// Entity
public class Document : IAuditableWithUserEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    // Auditing fields (automatically managed)
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

// DbContext
public class DocumentDbContext : ExtensibleDbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options, IServiceProvider serviceProvider)
        : base(options, serviceProvider) { }
    
    public DbSet<Document> Documents { get; set; }
}

// Configuration
builder.Services.AddExtensibleDbContext<DocumentDbContext>(
    options => options.UseSqlServer(connectionString),
    features => features.EnableAuditing()
);
```

### Automatic Current User Resolution

```csharp
// The library includes several user resolvers:

// 1. From JWT Claims (default)
builder.Services.AddCurrentUserResolver<ClaimsCurrentUserResolver>();

// 2. From HTTP headers
builder.Services.AddCurrentUserResolver<HttpHeaderCurrentUserResolver>();

// 3. Custom resolver
public class CustomUserResolver : ICurrentUserResolver
{
    public Task<string?> ResolveCurrentUserIdAsync()
    {
        // Your custom logic here
        return Task.FromResult<string?>("current-user-id");
    }
}

builder.Services.AddCustomCurrentUserResolver<CustomUserResolver>();
```

### Automatic Database Integration

```csharp
// Auditing fields are configured with default values:

// The processor automatically:
// 1. Sets CreatedDate and LastModifiedDate on INSERT
// 2. Updates LastModifiedDate on UPDATE  
// 3. Sets CreatedBy and ModifiedBy based on current user
// 4. Handles missing user scenarios according to configuration
```

### Automatic Filtering with Relationships

```csharp
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        // Only returns orders from current tenant
        // All included entities are also automatically filtered
        var orders = await _context.Orders
            .Include(o => o.Items)        // Items are filtered by tenant
            .ThenInclude(i => i.Product)  // Products are filtered by tenant
            .ToListAsync();

        return Ok(orders);
    }
}
```

### Direct Factory Access

```csharp
public class ReportService
{
    private readonly IMultiTenantDbContextFactory<ApplicationDbContext> _contextFactory;

    public ReportService(IMultiTenantDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<TenantReport> GenerateReportForTenant(string tenantId)
    {
        using var context = _contextFactory.CreateDbContext(tenantId);
        
        var productCount = await context.Products.CountAsync();
        var totalRevenue = await context.Orders.SumAsync(o => o.Total);
        
        return new TenantReport 
        { 
            TenantId = tenantId, 
            ProductCount = productCount, 
            TotalRevenue = totalRevenue 
        };
    }

    public async Task<CrossTenantReport> GenerateCrossTenantReport(List<string> tenantIds)
    {
        var tenantReports = new List<TenantReport>();
        
        foreach (var tenantId in tenantIds)
        {
            var report = await GenerateReportForTenant(tenantId);
            tenantReports.Add(report);
        }
        
        return new CrossTenantReport { TenantReports = tenantReports };
    }
}
```

## 🔧 Advanced Customization

### Custom User Resolver from JWT

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
        var context = _httpContextAccessor.HttpContext;
        var user = context?.User;
        
        // Try multiple claim types
        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user?.FindFirst("sub")?.Value
                  ?? user?.FindFirst("user_id")?.Value;
        
        return Task.FromResult(userId);
    }
}

// Register custom resolver
builder.Services.AddCustomCurrentUserResolver<JwtUserResolver>();
```

### Custom Tenant Resolver from Subdomains

```csharp
public class SubdomainTenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SubdomainTenantResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string?> ResolveTenantIdAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            var host = context.Request.Host.Host;
            var subdomain = host.Split('.').FirstOrDefault();
            
            // tenant1.myapp.com -> tenant1
            if (!string.IsNullOrEmpty(subdomain) && subdomain != "www")
            {
                return Task.FromResult<string?>(subdomain);
            }
        }

        return Task.FromResult<string?>(null);
    }
}

// Register custom resolver
builder.Services.AddCustomTenantResolver<SubdomainTenantResolver>();
```

### Custom Feature Processor

```csharp
// Create a custom feature processor for soft deletes
public class SoftDeleteProcessor : IFeatureProcessor
{
    public void OnModelCreating(ModelBuilder modelBuilder, Type entityType)
    {
        if (typeof(ISoftDeletable).IsAssignableFrom(entityType))
        {
            // Configure soft delete filters
            var method = typeof(SoftDeleteProcessor)
                .GetMethod(nameof(ConfigureSoftDelete), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType);
            method.Invoke(this, new object[] { modelBuilder });
        }
    }

    private void ConfigureSoftDelete<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    public void OnSaveChanges(ChangeTracker changeTracker)
    {
        foreach (var entry in changeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedDate = DateTime.UtcNow;
            }
        }
    }
}

// Define the interface
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedDate { get; set; }
}

// Register the processor
// (Future version will support custom feature registration)
```

### Environment-Specific Configuration

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp;",
    "Tenant1": "Server=localhost;Database=MyApp_Tenant1;",
    "Tenant2": "Server=localhost;Database=MyApp_Tenant2;"
  },
  "MultiTenant": {
    "Strategy": "Database", // or "Discriminator"
    "EnableAuditing": true,
    "AuditingBehavior": "UseDefaultUser",
    "DefaultUserId": "SYSTEM",
    "Tenants": [
      {
        "Id": "tenant1",
        "Name": "Tenant 1",
        "ConnectionString": "Tenant1"
      },
      {
        "Id": "tenant2", 
        "Name": "Tenant 2",
        "ConnectionString": "Tenant2"
      }
    ]
  }
}

// Program.cs
var multiTenantConfig = builder.Configuration.GetSection("MultiTenant");
var strategy = multiTenantConfig["Strategy"];
var enableAuditing = multiTenantConfig.GetValue<bool>("EnableAuditing");

if (strategy == "Database")
{
    var tenants = multiTenantConfig.GetSection("Tenants").Get<TenantConfiguration[]>();
    
    builder.Services.AddMultiTenantByDatabaseDbContext<ApplicationDbContext>(
        tenantBuilder => 
        {
            foreach (var tenant in tenants)
            {
                var connectionString = builder.Configuration.GetConnectionString(tenant.ConnectionString);
                tenantBuilder.AddTenant(tenant.Id, options => options.UseSqlServer(connectionString));
            }
            return tenantBuilder;
        },
        features => 
        {
            if (enableAuditing)
            {
                var behavior = multiTenantConfig["AuditingBehavior"];
                var defaultUser = multiTenantConfig["DefaultUserId"];
                
                features.EnableAuditing(audit => 
                {
                    audit.BehaviorWhenNoUser = Enum.Parse<AuditingBehavior>(behavior);
                    audit.DefaultUserId = defaultUser;
                });
            }
            return features;
        }
    );
}
else
{
    builder.Services.AddMultiTenantByDiscriminatorDbContext<ApplicationDbContext>(
        options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")),
        features => enableAuditing ? features.EnableAuditing() : features
    );
}

public class TenantConfiguration
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}
```

## 📖 API Reference

### Main Interfaces

#### Tenant Interfaces
```csharp
// Read-only tenant store
public interface ITenantStore
{
    string? GetCurrentTenantId();
}

// Read/Write tenant store
public interface IWritableTenantStore : ITenantStore
{
    void SetCurrentTenantId(string tenantId);
    void ClearCurrentTenantId();
}

// Tenant resolver
public interface ITenantResolver
{
    Task<string?> ResolveTenantIdAsync();
}

// Tenant entity marker
public interface ITenantEntity
{
    string TenantId { get; set; }
}
```

#### User Interfaces
```csharp
// Read-only user store
public interface ICurrentUserStore
{
    string? GetCurrentUserId();
}

// Read/Write user store  
public interface IWritableCurrentUserStore : ICurrentUserStore
{
    void SetCurrentUserId(string userId);
    void ClearCurrentUserId();
}

// User resolver
public interface ICurrentUserResolver
{
    Task<string?> ResolveCurrentUserIdAsync();
}
```

#### Auditing Interfaces
```csharp
// Basic auditing (dates only)
public interface IAuditableEntity
{
    DateTime CreatedDate { get; set; }
    DateTime LastModifiedDate { get; set; }
}

// Full auditing (dates + users)
public interface IAuditableWithUserEntity : IAuditableEntity  
{
    string? CreatedBy { get; set; }
    string? ModifiedBy { get; set; }
}
```

#### Factory Interface
```csharp
public interface IMultiTenantDbContextFactory<TContext> where TContext : DbContext
{
    TContext CreateDbContext();
    TContext CreateDbContext(string tenantId);
}
```

### Extension Methods

#### Service Configuration
```csharp
// Simple extensible context
services.AddExtensibleDbContext<TContext>(
    Action<DbContextOptionsBuilder> configureOptions,
    Func<DbContextFeaturesBuilder, DbContextFeaturesBuilder>? featuresBuilder = null)

// Multi-tenant discriminator
services.AddMultiTenantByDiscriminatorDbContext<TContext>(
    Action<DbContextOptionsBuilder<TContext>> configureOptions,
    Func<DbContextFeaturesBuilder, DbContextFeaturesBuilder>? featuresBuilder = null)

// Multi-tenant database  
services.AddMultiTenantByDatabaseDbContext<TContext>(
    Action<MultiTenantByDatabaseBuilder<TContext>> buildTenants,
    Func<DbContextFeaturesBuilder, DbContextFeaturesBuilder>? featuresBuilder = null)

// Custom resolvers and stores
services.AddCustomTenantResolver<TResolver>()
services.AddCustomTenantStore<TStore>() 
services.AddCustomCurrentUserResolver<TResolver>()
services.AddCustomCurrentUserStore<TStore>()
```

#### Pipeline Configuration
```csharp
// Automatic middleware (activates only when necessary)
app.UseCurrentUser() // For user tracking
app.UseMultiTenant() // For tenant isolation
```

### Base Classes

#### ExtensibleDbContext
Base class for contexts with feature support:
- Automatic processing of features during model creation
- Automatic feature execution during SaveChanges
- Support for dependency injection of feature processors

#### MultiTenantDbContext
Inherits from ExtensibleDbContext and adds:
- Automatic entity filtering by tenant
- Automatic TenantId injection in SaveChanges
- Access to current tenant via `CurrentTenantId`

### Features System

#### DbContextFeatures Configuration
```csharp
public class DbContextFeatures
{
    public bool AuditingEnabled { get; set; }
    public AuditingConfiguration AuditingConfiguration { get; set; }
}

public class DbContextFeaturesBuilder
{
    public DbContextFeaturesBuilder EnableAuditing(Action<AuditingConfiguration>? configureAuditing = null)
}
```

#### Auditing Configuration
```csharp
public class AuditingConfiguration
{
    public AuditingBehavior BehaviorWhenNoUser { get; set; } = AuditingBehavior.SaveAsNull;
    public string? DefaultUserId { get; set; } = "system";

    // Predefined configurations
    public static AuditingConfiguration Default { get; }
    public static AuditingConfiguration ThrowOnMissingUser { get; }
    public static AuditingConfiguration UseSystemUser(string systemUserId = "system") { get; }
    public static AuditingConfiguration SkipUserFields { get; }
}

public enum AuditingBehavior
{
    ThrowException,  // Throws exception if can't resolve user
    UseDefaultUser,  // Uses default user if can't resolve user  
    SaveAsNull,      // Saves null if can't resolve user
    SkipUserFields   // Skip user fields leaving them as they are
}
```

### Included Implementations

#### Stores
- `InMemoryTenantStore`: In-memory tenant store using AsyncLocal (default)
- `InMemoryCurrentUserStore`: In-memory user store using AsyncLocal (default)
- `ClaimsTenantStore`: Claims-based tenant store (read-only)

#### Resolvers
- `HttpHeaderTenantResolver`: Resolves tenant from HTTP header (default: "X-Tenant-Id")
- `HttpHeaderCurrentUserResolver`: Resolves user from HTTP header (default: "X-User-Id")
- `ClaimsCurrentUserResolver`: Resolves user from JWT claims (default: NameIdentifier)

## 🎯 Best Practices

### 1. Error Handling with Comprehensive Middleware

```csharp
public class TenantErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantErrorHandlingMiddleware> _logger;

    public TenantErrorHandlingMiddleware(RequestDelegate next, ILogger<TenantErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Current tenant ID is not set"))
        {
            _logger.LogWarning("Tenant ID not provided for request {RequestPath}", context.Request.Path);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant ID required" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Current user ID is required"))
        {
            _logger.LogWarning("User ID required but not available for request {RequestPath}", context.Request.Path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "User authentication required" });
        }
    }
}

// Register before other middleware
app.UseMiddleware<TenantErrorHandlingMiddleware>();
app.UseCurrentUser();
app.UseMultiTenant();
```

### 2. Structured Logging with Context

```csharp
public class ContextLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ContextLoggingMiddleware> _logger;

    public ContextLoggingMiddleware(RequestDelegate next, ILogger<ContextLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantStore = context.RequestServices.GetService<ITenantStore>();
        var userStore = context.RequestServices.GetService<ICurrentUserStore>();
        
        var tenantId = tenantStore?.GetCurrentTenantId();
        var userId = userStore?.GetCurrentUserId();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TenantId"] = tenantId ?? "Unknown",
            ["UserId"] = userId ?? "Anonymous",
            ["RequestId"] = Activity.Current?.Id ?? context.TraceIdentifier
        }))
        {
            _logger.LogInformation("Processing request {Method} {Path}");
            await _next(context);
            _logger.LogInformation("Completed request {Method} {Path} with {StatusCode}");
        }
    }
}
```

### 3. Tenant and User Validation Attributes

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireContextAttribute : Attribute, IActionFilter
{
    public bool RequireTenant { get; set; } = true;
    public bool RequireUser { get; set; } = false;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var serviceProvider = context.HttpContext.RequestServices;
        
        if (RequireTenant)
        {
            var tenantStore = serviceProvider.GetRequiredService<ITenantStore>();
            var tenantId = tenantStore.GetCurrentTenantId();
            
            if (string.IsNullOrEmpty(tenantId))
            {
                context.Result = new BadRequestObjectResult(new { error = "Tenant required" });
                return;
            }
        }
        
        if (RequireUser)
        {
            var userStore = serviceProvider.GetService<ICurrentUserStore>();
            var userId = userStore?.GetCurrentUserId();
            
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "User authentication required" });
                return;
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}

// Usage in controllers
[RequireContext(RequireTenant = true, RequireUser = true)]
public class ProductsController : ControllerBase
{
    // All methods require both tenant and user
}

[RequireContext(RequireTenant = false, RequireUser = true)]
public class UserProfileController : ControllerBase  
{
    // Single tenant app, but requires authenticated user
}
```

### 4. Advanced Migration Management

```csharp
public class AdvancedMigrationService
{
    private readonly IMultiTenantDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<AdvancedMigrationService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AdvancedMigrationService(
        IMultiTenantDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<AdvancedMigrationService> logger,
        IServiceProvider serviceProvider)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAllTenantsAsync(IEnumerable<string> tenantIds, CancellationToken cancellationToken = default)
    {
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
        var tasks = tenantIds.Select(async tenantId =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await MigrateTenantAsync(tenantId, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(tasks);
    }

    private async Task MigrateTenantAsync(string tenantId, CancellationToken cancellationToken)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext(tenantId);
            
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pendingMigrations.Any())
            {
                _logger.LogInformation("Applying {Count} migrations for tenant {TenantId}: {Migrations}", 
                    pendingMigrations.Count(), tenantId, string.Join(", ", pendingMigrations));
                
                await context.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Successfully migrated tenant {TenantId}", tenantId);
            }
            else
            {
                _logger.LogInformation("No pending migrations for tenant {TenantId}", tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<MigrationStatus> GetMigrationStatusAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext(tenantId);
            
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
            
            return new MigrationStatus
            {
                TenantId = tenantId,
                AppliedMigrations = appliedMigrations.ToList(),
                PendingMigrations = pendingMigrations.ToList(),
                IsUpToDate = !pendingMigrations.Any()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get migration status for tenant {TenantId}", tenantId);
            throw;
        }
    }
}

public class MigrationStatus
{
    public string TenantId { get; set; } = string.Empty;
    public List<string> AppliedMigrations { get; set; } = new();
    public List<string> PendingMigrations { get; set; } = new();
    public bool IsUpToDate { get; set; }
}
```

### 5. Health Checks for Multi-Tenant Applications

```csharp
public class MultiTenantHealthCheck : IHealthCheck
{
    private readonly IMultiTenantDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IEnumerable<string> _tenantIds;
    private readonly ILogger<MultiTenantHealthCheck> _logger;

    public MultiTenantHealthCheck(
        IMultiTenantDbContextFactory<ApplicationDbContext> contextFactory,
        IConfiguration configuration,
        ILogger<MultiTenantHealthCheck> logger)
    {
        _contextFactory = contextFactory;
        _tenantIds = configuration.GetSection("MultiTenant:Tenants").Get<string[]>() ?? Array.Empty<string>();
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var healthyTenants = new List<string>();
        var unhealthyTenants = new List<string>();

        foreach (var tenantId in _tenantIds)
        {
            try
            {
                using var dbContext = _contextFactory.CreateDbContext(tenantId);
                await dbContext.Database.CanConnectAsync(cancellationToken);
                healthyTenants.Add(tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for tenant {TenantId}", tenantId);
                unhealthyTenants.Add(tenantId);
            }
        }

        var data = new Dictionary<string, object>
        {
            ["TotalTenants"] = _tenantIds.Count(),
            ["HealthyTenants"] = healthyTenants.Count,
            ["UnhealthyTenants"] = unhealthyTenants.Count,
            ["HealthyTenantIds"] = healthyTenants,
            ["UnhealthyTenantIds"] = unhealthyTenants
        };

        if (unhealthyTenants.Any())
        {
            return HealthCheckResult.Degraded($"{unhealthyTenants.Count} out of {_tenantIds.Count()} tenants are unhealthy", data: data);
        }

        return HealthCheckResult.Healthy($"All {healthyTenants.Count} tenants are healthy", data: data);
    }
}

// Register health checks
builder.Services.AddHealthChecks()
    .AddCheck<MultiTenantHealthCheck>("multi-tenant-db");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

## 🧪 Testing

The library includes a comprehensive test suite covering unit and integration tests.

### Running Tests

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test CdCSharp.EF.UnitTests

# Run integration tests only
dotnet test CdCSharp.EF.IntegrationTests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test categories
dotnet test --filter "Category=MultiTenant"
dotnet test --filter "Category=Auditing"
dotnet test --filter "Category=Features"
```

### Test Structure

#### Unit Tests
- **AuditingProcessorTests**: Tests for automatic auditing functionality
- **MultiTenantDbContextTests**: Tests for tenant filtering and isolation
- **ExtensibleDbContextTests**: Tests for feature processing
- **MiddlewareTests**: Tests for tenant and user middleware
- **ResolverTests**: Tests for tenant and user resolvers
- **StoreTests**: Tests for tenant and user stores
- **ServiceCollectionExtensionsTests**: Tests for DI configuration

#### Integration Tests
- **DiscriminatorMultiTenantTests**: End-to-end tests with discriminator strategy
- **DatabaseMultiTenantTests**: End-to-end tests with database strategy
- **ClaimsBasedMultiTenantTests**: Tests with claims-based tenant resolution
- **AuditingIntegrationTests**: End-to-end tests for auditing features
- **ExtensibleDbContextTests**: Integration tests for simple features

### Custom Test Examples

#### Testing Multi-Tenant Services

```csharp
public class ProductServiceTests
{
    [Fact]
    public async Task GetProducts_WithSpecificTenant_ReturnsOnlyTenantProducts()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        var mockTenantStore = new Mock<ITenantStore>();
        mockTenantStore.Setup(s => s.GetCurrentTenantId()).Returns("tenant1");

        var services = new ServiceCollection();
        services.AddSingleton(mockTenantStore.Object);
        services.AddSingleton(DbContextFeatures.Default);
        var serviceProvider = services.BuildServiceProvider();

        await using var context = new ApplicationDbContext(options, serviceProvider);
        
        // Seed test data
        await context.Products.AddRangeAsync(
            new Product { Name = "Product 1", TenantId = "tenant1", Price = 100 },
            new Product { Name = "Product 2", TenantId = "tenant2", Price = 200 },
            new Product { Name = "Product 3", TenantId = "tenant1", Price = 300 }
        );
        await context.SaveChangesAsync();

        var service = new ProductService(context);

        // Act
        var products = await service.GetProductsAsync();

        // Assert
        Assert.Equal(2, products.Count);
        Assert.All(products, p => Assert.Equal("tenant1", p.TenantId));
    }
}
```

#### Testing Auditing Features

```csharp
public class AuditingFeatureTests
{
    [Fact]
    public async Task CreateProduct_WithAuditingEnabled_SetsAuditFields()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        var mockUserStore = new Mock<ICurrentUserStore>();
        mockUserStore.Setup(s => s.GetCurrentUserId()).Returns("testuser");

        var services = new ServiceCollection();
        services.AddSingleton(mockUserStore.Object);
        services.AddSingleton(new DbContextFeatures { AuditingEnabled = true });
        services.AddScoped<IFeatureProcessor, AuditingProcessor>();
        var serviceProvider = services.BuildServiceProvider();

        await using var context = new ApplicationDbContext(options, serviceProvider);
        await context.Database.EnsureCreatedAsync();

        // Act
        var product = new Product { Name = "Test Product", Price = 99.99m };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Assert
        Assert.True(product.CreatedDate > DateTime.MinValue);
        Assert.True(product.LastModifiedDate > DateTime.MinValue);
        Assert.Equal("testuser", product.CreatedBy);
        Assert.Equal("testuser", product.ModifiedBy);
    }
}
```

#### Testing Custom Resolvers

```csharp
public class CustomResolverTests
{
    [Fact]
    public async Task CustomTenantResolver_WithSubdomain_ReturnsCorrectTenant()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();
        
        mockRequest.Setup(r => r.Host).Returns(new HostString("tenant1.myapp.com"));
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var resolver = new SubdomainTenantResolver(mockHttpContextAccessor.Object);

        // Act
        var tenantId = await resolver.ResolveTenantIdAsync();

        // Assert
        Assert.Equal("tenant1", tenantId);
    }
}
```

### Testing Best Practices

1. **Use in-memory databases** for fast, isolated tests
2. **Mock external dependencies** (HTTP context, user stores, etc.)
3. **Test tenant isolation** thoroughly in multi-tenant scenarios
4. **Verify auditing behavior** in all configured scenarios
5. **Test error conditions** (missing tenant, missing user, etc.)
6. **Use descriptive test names** that explain the scenario
7. **Group related tests** using nested classes or categories

## 🤝 Contributing

Contributions are welcome! To contribute:

### Development Setup

1. **Fork the project** and clone your fork
2. **Install .NET 9 SDK** 
3. **Restore dependencies**: `dotnet restore`
4. **Build the project**: `dotnet build`
5. **Run tests**: `dotnet test`

### Contribution Guidelines

- **Follow existing code conventions** and patterns
- **Write comprehensive tests** for new functionality
- **Update documentation** for any API changes
- **Keep backwards compatibility** when possible
- **Use meaningful commit messages**

### Development Workflow

1. Create a feature branch (`git checkout -b feature/amazing-feature`)
2. Make your changes with tests
3. Ensure all tests pass (`dotnet test`)
4. Update relevant documentation
5. Commit your changes (`git commit -m 'Add some amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)  
7. Open a Pull Request

### Areas for Contribution

- **New feature processors** (soft delete, versioning, etc.)
- **Additional resolvers** (query string, route values, etc.)
- **Performance optimizations**
- **Documentation improvements**
- **Example applications**
- **Additional test scenarios**

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙋‍♂️ Support

For questions, bug reports, or feature requests:

- **Issues**: [Open an issue](https://github.com/smaicas/CdCSharp.EF/issues)
- **Documentation**: [View complete docs](https://github.com/smaicas/CdCSharp.EF/wiki)  
- **Examples**: [Browse examples](https://github.com/smaicas/CdCSharp.EF/tree/main/examples)
- **Discussions**: [Join discussions](https://github.com/smaicas/CdCSharp.EF/discussions)

## 🌟 Acknowledgments

- Built on **Entity Framework Core** and **.NET 9**
- Inspired by **enterprise multi-tenancy patterns**
- Designed with **developer experience** in mind
- **Community-driven** development and feedback

---

⭐ **If this library has been helpful, please give it a star!**

[![Stars](https://img.shields.io/github/stars/smaicas/CdCSharp.EF?style=social)](https://github.com/smaicas/CdCSharp.EF/stargazers)

---

## 🔄 Version History

### v1.0.0
- ✅ Initial release with multi-tenancy support
- ✅ Discriminator and Database strategies
- ✅ Automatic filtering and tenant injection
- ✅ Extensible resolver and store system

### v1.1.0 (Current)
- ✅ **Automatic auditing system** with configurable behaviors
- ✅ **Current user tracking** with multiple resolution strategies  
- ✅ **Extensible features architecture** for custom processors
- ✅ **Enhanced builders** for cleaner configuration
- ✅ **Comprehensive testing suite** with integration tests
- ✅ **Improved middleware** that only activates when needed
- ✅ **Better error handling** and logging support