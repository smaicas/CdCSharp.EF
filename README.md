# CdCSharp.EF - Multi-Tenant Library

[![NuGet](https://img.shields.io/nuget/v/CdCSharp.FluentCli.svg)](https://www.nuget.org/packages/CdCSharp.EF)
[![License](https://img.shields.io/github/license/smaicas/CdCSharp.EF)](LICENSE)
[![Build Status](https://img.shields.io/github/actions/workflow/status/smaicas/CdCSharp.EF/dotnet.yml?branch=master)](https://github.com/smaicas/CdCSharp.EF/actions/workflows/dotnet.yml)

A complete and extensible library for implementing Multi-Tenancy in .NET 9 applications with Entity Framework Core, offering transparent data isolation and flexible configuration.

## 📚 Table of Contents

- [Features](#-features)
- [Installation](#-installation)
- [Quick Setup](#-quick-setup)
- [Multi-Tenancy Strategies](#-multi-tenancy-strategies)
- [Basic Usage](#-basic-usage)
- [Advanced Customization](#-advanced-customization)
- [API Reference](#-api-reference)
- [Best Practices](#-best-practices)
- [Testing](#-testing)
- [Contributing](#-contributing)

## ✨ Features

- ✅ **Two Multi-Tenancy strategies**: By Discriminator (single DB) and By Database (multiple DBs)
- ✅ **Automatic filtering**: Automatic query filters per tenant with relationship support
- ✅ **Flexible resolution**: Extensible system to resolve current tenant (Headers, Claims, Query String, etc.)
- ✅ **Configurable stores**: Tenant storage with in-memory and claims-based implementations
- ✅ **Smart middleware**: Only activates when necessary (writable stores)
- ✅ **Fluent APIs**: Simple and clear configuration with extension methods
- ✅ **Automatic injection**: Automatic TenantId establishment in entities
- ✅ **Type-safe**: Separate interfaces for read-only vs read/write stores
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

public class Product : ITenantEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string TenantId { get; set; } = string.Empty; // Required by ITenantEntity
}

public class Order : ITenantEntity
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public string TenantId { get; set; } = string.Empty;
}
```

### 2. Create your DbContext

```csharp
using CdCSharp.EF.Core;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : MultiTenantDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
}
```

### 3. Configure services in Program.cs

```csharp
using CdCSharp.EF.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Discriminator Strategy (recommended to start with)
builder.Services.AddMultiTenantByDiscriminatorDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Register middleware (automatically activates only when necessary)
app.UseMultiTenant();
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
            // TenantId is set automatically
        };
        
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
    }
}
```

### 5. Use with HTTP headers

```bash
# Tenant is automatically resolved from header
curl -H "X-Tenant-Id: tenant1" https://api.example.com/api/products
```

## 🏗️ Multi-Tenancy Strategies

### Discriminator Strategy

A single database with automatic filtering by `TenantId`. **Recommended** for most use cases.

```csharp
// Simple configuration
builder.Services.AddMultiTenantByDiscriminatorDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
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
// Multiple configuration for different tenants
builder.Services.AddMultiTenantByDatabaseDbContext<ApplicationDbContext>("tenant1", options =>
    options.UseSqlServer(tenant1ConnectionString));

builder.Services.AddMultiTenantByDatabaseDbContext<ApplicationDbContext>("tenant2", options =>
    options.UseSqlServer(tenant2ConnectionString));

builder.Services.AddMultiTenantByDatabaseDbContext<ApplicationDbContext>("tenant3", options =>
    options.UseSqlServer(tenant3ConnectionString));
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

## 💡 Basic Usage

### Automatic Filtering

```csharp
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        // Only returns orders from current tenant
        var orders = await _context.Orders
            .Include(o => o.Items)        // Relationships are also automatically filtered
            .ThenInclude(i => i.Product)  // by tenant
            .ToListAsync();

        return Ok(orders);
    }
}
```

### Automatic TenantId Creation

```csharp
[HttpPost]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
{
    var order = new Order
    {
        CustomerName = dto.CustomerName,
        OrderDate = DateTime.UtcNow,
        Items = dto.Items.Select(i => new OrderItem 
        { 
            ProductId = i.ProductId, 
            Quantity = i.Quantity 
            // TenantId is set automatically in SaveChanges
        }).ToList()
        // TenantId is set automatically in SaveChanges
    };

    _context.Orders.Add(order);
    await _context.SaveChangesAsync(); // TenantId is assigned here

    return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
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
        
        return new TenantReport { TenantId = tenantId, ProductCount = productCount, TotalRevenue = totalRevenue };
    }
}
```

## 🔧 Advanced Customization

### Custom Resolver from Subdomains

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

### Claims-Based Store (for JWT)

```csharp
// Already included in the library
builder.Services.AddCustomTenantStore<ClaimsTenantStore>();

// Include "tenant-id" claim in JWT token
// The store will automatically read it from the authenticated user
```

### Custom Store with Redis

```csharp
public class RedisTenantStore : IWritableTenantStore
{
    private readonly IDistributedCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RedisTenantStore(IDistributedCache cache, IHttpContextAccessor httpContextAccessor)
    {
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentTenantId()
    {
        var sessionId = GetSessionId();
        return _cache.GetString($"tenant:{sessionId}");
    }

    public void SetCurrentTenantId(string tenantId)
    {
        var sessionId = GetSessionId();
        _cache.SetString($"tenant:{sessionId}", tenantId, TimeSpan.FromHours(24));
    }

    public void ClearCurrentTenantId()
    {
        var sessionId = GetSessionId();
        _cache.Remove($"tenant:{sessionId}");
    }

    private string GetSessionId()
    {
        return _httpContextAccessor.HttpContext?.Session.Id ?? Guid.NewGuid().ToString();
    }
}

// Register custom store
builder.Services.AddCustomTenantStore<RedisTenantStore>();
```

### Resolver from Query String

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
            return Task.FromResult<string?>(tenantValues.FirstOrDefault());
        }

        return Task.FromResult<string?>(null);
    }
}

// Usage: /api/products?tenant=tenant1
```

## 📖 API Reference

### Main Interfaces

#### ITenantStore (Read-Only)
```csharp
public interface ITenantStore
{
    string? GetCurrentTenantId();
}
```

#### IWritableTenantStore (Read/Write)
```csharp
public interface IWritableTenantStore : ITenantStore
{
    void SetCurrentTenantId(string tenantId);
    void ClearCurrentTenantId();
}
```

#### ITenantResolver
```csharp
public interface ITenantResolver
{
    Task<string?> ResolveTenantIdAsync();
}
```

#### ITenantEntity
```csharp
public interface ITenantEntity
{
    string TenantId { get; set; }
}
```

#### IMultiTenantDbContextFactory
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
// Discriminator strategy
services.AddMultiTenantByDiscriminatorDbContext<TContext>(Action<DbContextOptionsBuilder<TContext>> configureOptions)

// Database strategy
services.AddMultiTenantByDatabaseDbContext<TContext>(string tenantId, Action<DbContextOptionsBuilder<TContext>> configureOptions)

// Custom resolvers and stores
services.AddCustomTenantResolver<TResolver>()
services.AddCustomTenantStore<TStore>()
```

#### Pipeline Configuration
```csharp
// Automatic middleware (activates only when necessary)
app.UseMultiTenant()
```

### Base Classes

#### MultiTenantDbContext
Abstract base class that provides:
- Automatic entity filtering by tenant
- Automatic TenantId injection in SaveChanges
- Access to current tenant via `CurrentTenantId`

### Included Implementations

#### Stores
- `InMemoryTenantStore`: In-memory store using AsyncLocal (default)
- `ClaimsTenantStore`: Claims-based authentication store (read-only)

#### Resolvers
- `HttpHeaderTenantResolver`: Resolves from HTTP header (default: "X-Tenant-Id")

## 🎯 Best Practices

### 1. Error Handling

```csharp
// Custom middleware for tenant errors
public class TenantErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public TenantErrorHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Current tenant ID is not set"))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant ID required" });
        }
    }
}

// Register before multi-tenant middleware
app.UseMiddleware<TenantErrorHandlingMiddleware>();
app.UseMultiTenant();
```

### 2. Logging with Tenant Context

```csharp
public class TenantLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantLoggingMiddleware> _logger;

    public TenantLoggingMiddleware(RequestDelegate next, ILogger<TenantLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantStore = context.RequestServices.GetRequiredService<ITenantStore>();
        var tenantId = tenantStore.GetCurrentTenantId();

        using (_logger.BeginScope("TenantId: {TenantId}", tenantId))
        {
            await _next(context);
        }
    }
}
```

### 3. Tenant Validation

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireTenantAttribute : Attribute, IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var tenantStore = context.HttpContext.RequestServices.GetRequiredService<ITenantStore>();
        var tenantId = tenantStore.GetCurrentTenantId();

        if (string.IsNullOrEmpty(tenantId))
        {
            context.Result = new BadRequestObjectResult(new { error = "Tenant required" });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}

// Usage in controllers
[RequireTenant]
public class ProductsController : ControllerBase
{
    // All methods will require tenant
}
```

### 4. Migrations for Database Strategy

```csharp
public class DatabaseMigrationService
{
    private readonly IMultiTenantDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(
        IMultiTenantDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<DatabaseMigrationService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task MigrateAllTenantsAsync(IEnumerable<string> tenantIds)
    {
        var tasks = tenantIds.Select(MigrateTenantAsync);
        await Task.WhenAll(tasks);
    }

    private async Task MigrateTenantAsync(string tenantId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext(tenantId);
            await context.Database.MigrateAsync();
            _logger.LogInformation("Migration successful for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed for tenant {TenantId}", tenantId);
            throw;
        }
    }
}
```

### 5. Environment Configuration

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
    "DefaultTenant": "tenant1",
    "Tenants": ["tenant1", "tenant2", "tenant3"]
  }
}

// Program.cs
var config = builder.Configuration.GetSection("MultiTenant");
var strategy = config["Strategy"];
var tenants = config.GetSection("Tenants").Get<string[]>();

if (strategy == "Database")
{
    foreach (var tenant in tenants)
    {
        var connectionString = builder.Configuration.GetConnectionString(tenant) 
                              ?? builder.Configuration.GetConnectionString("DefaultConnection");
        
        builder.Services.AddMultiTenantByDatabaseDbContext<ApplicationDbContext>(tenant, options =>
            options.UseSqlServer(connectionString));
    }
}
else
{
    builder.Services.AddMultiTenantByDiscriminatorDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}
```

## 🧪 Testing

The library includes a complete suite of unit and integration tests.

### Unit Tests

```bash
# Run unit tests
dotnet test CdCSharp.EF.UnitTests

# With coverage
dotnet test CdCSharp.EF.UnitTests --collect:"XPlat Code Coverage"
```

### Integration Tests

```bash
# Run all integration tests
dotnet test CdCSharp.EF.IntegrationTests

# Strategy-specific tests
dotnet test --filter "DiscriminatorMultiTenant"
dotnet test --filter "DatabaseMultiTenant"
dotnet test --filter "ClaimsBasedMultiTenant"
```

### Custom Test Example

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

        await using var context = new ApplicationDbContext(options, Mock.Of<IServiceProvider>());
        
        // Seed test data
        context.Products.AddRange(
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

## 🤝 Contributing

Contributions are welcome. To contribute:

1. Fork the project
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Contribution Guidelines

- Ensure all tests pass
- Add tests for new functionality
- Follow established code conventions
- Update documentation as needed

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙋‍♂️ Support

To report bugs or request new features:

- [Open an issue](https://github.com/your-username/CdCSharp.EF/issues)
- [View complete documentation](https://github.com/your-username/CdCSharp.EF/wiki)
- [Additional examples](https://github.com/your-username/CdCSharp.EF/tree/main/examples)

## 🌟 Acknowledgments

- Based on .NET community multi-tenancy best practices
- Inspired by real enterprise application needs
- Built with love for the developer community

---

⭐ If this project has been useful to you, don't forget to give it a star!

[![Stars](https://img.shields.io/github/stars/smaicas/CdCSharp.EF?style=social)](https://github.com/smaicas/CdCSharp.EF/stargazers)