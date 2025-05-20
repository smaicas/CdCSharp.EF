using CdCSharp.EF.Configuration;
using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CdCSharp.EF.IntegrationTests.Env;

public class DiscriminatorMultiTenantWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public DiscriminatorMultiTenantWebApplicationFactory() => _databaseName = $"Integration_Discriminator_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        builder.ConfigureServices(services =>
        {
            services.AddHttpContextAccessor();

            // Add controllers
            services.AddControllers();

            // Configure multi-tenant with discriminator strategy
            services.AddMultiTenantByDiscriminatorDbContext<IntegrationTestDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });

        builder.Configure(app =>
        {
            app.UseMultiTenant();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        });
    }

    public async Task SeedDataAsync(string tenantId, Func<IntegrationTestDbContext, Task> seedAction)
    {
        using IServiceScope scope = Services.CreateScope();

        Core.Abstractions.IWritableTenantStore? tenantStore = scope.ServiceProvider.GetRequiredService<Core.Abstractions.ITenantStore>() as IWritableTenantStore;

        tenantStore!.SetCurrentTenantId(tenantId);
        IntegrationTestDbContext context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();
        await context.Database.EnsureCreatedAsync();
        await seedAction(context);

        tenantStore!.ClearCurrentTenantId();
    }
}

// Factory para tests discriminator con features
public class DiscriminatorWithFeaturesMultiTenantWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public DiscriminatorWithFeaturesMultiTenantWebApplicationFactory() =>
        _databaseName = $"Integration_DiscriminatorFeatures_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        builder.ConfigureServices(services =>
        {
            services.AddHttpContextAccessor();

            // Add controllers
            services.AddControllers();

            // Configure multi-tenant with discriminator strategy and features
            services.AddMultiTenantByDiscriminatorDbContext<IntegrationTestDbContext>(
                options => options.UseInMemoryDatabase(_databaseName),
                features => features.EnableAuditing(config =>
                {
                    config.BehaviorWhenNoUser = AuditingBehavior.UseDefaultUser;
                    config.DefaultUserId = "SYSTEM";
                })
            );
        });

        builder.Configure(app =>
        {
            app.UseCurrentUser();
            app.UseMultiTenant();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        });
    }

    public async Task SeedDataAsync(string tenantId, Func<IntegrationTestDbContext, Task> seedAction)
    {
        using IServiceScope scope = Services.CreateScope();

        IWritableTenantStore? tenantStore = scope.ServiceProvider.GetRequiredService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId(tenantId);

        IntegrationTestDbContext context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();
        await context.Database.EnsureCreatedAsync();
        await seedAction(context);

        tenantStore!.ClearCurrentTenantId();
    }
}

// Factory para tests con estrategia Database
public class DatabaseMultiTenantWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _instanceId = Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        builder.ConfigureServices(services =>
        {
            services.AddHttpContextAccessor();

            // Add controllers
            services.AddControllers();

            // Usar el nuevo método con builder pattern y features
            services.AddMultiTenantByDatabaseDbContext<IntegrationTestDbContext>(
                tenants => tenants
                    .AddTenant("tenant1", options =>
                        options.UseInMemoryDatabase($"Integration_DB_tenant1_{_instanceId}"))
                    .AddTenant("tenant2", options =>
                        options.UseInMemoryDatabase($"Integration_DB_tenant2_{_instanceId}"))
                    .AddTenant("tenant3", options =>
                        options.UseInMemoryDatabase($"Integration_DB_tenant3_{_instanceId}")),
                features => features.EnableAuditing(config =>
                {
                    config.BehaviorWhenNoUser = AuditingBehavior.UseDefaultUser;
                    config.DefaultUserId = "SYSTEM";
                })
            );
        });

        builder.Configure(app =>
        {
            app.UseCurrentUser();
            app.UseMultiTenant();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        });
    }

    public async Task SeedDataForTenantAsync(string tenantId, Func<IntegrationTestDbContext, Task> seedAction)
    {
        using IServiceScope scope = Services.CreateScope();
        IMultiTenantDbContextFactory<IntegrationTestDbContext> factory = scope.ServiceProvider.GetRequiredService<IMultiTenantDbContextFactory<IntegrationTestDbContext>>();

        IWritableTenantStore? tenantStore = scope.ServiceProvider.GetRequiredService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId(tenantId);

        using IntegrationTestDbContext context = factory.CreateDbContext(tenantId);
        await context.Database.EnsureCreatedAsync();
        await seedAction(context);
    }
}

// Factory para tests con resolver personalizado (Claims-based)
public class ClaimsBasedMultiTenantWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public ClaimsBasedMultiTenantWebApplicationFactory() => _databaseName = $"Integration_Claims_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        builder.ConfigureServices(services =>
        {
            services.AddHttpContextAccessor();

            // Add authentication for claims
            services.AddAuthentication("Test")
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "Test", options => { });

            // Add controllers
            services.AddControllers();

            // Configure multi-tenant with discriminator strategy
            services.AddMultiTenantByDiscriminatorDbContext<IntegrationTestDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Use claims-based tenant store
            services.AddCustomTenantStore<Core.Stores.ClaimsTenantStore>();
        });

        builder.Configure(app =>
        {
            app.UseAuthentication();
            app.UseMultiTenant();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        });
    }

    public async Task SeedDataAsync(string tenantId, Func<IntegrationTestDbContext, Task> seedAction)
    {
        using IServiceScope scope = Services.CreateScope();

        IMultiTenantDbContextFactory<IntegrationTestDbContext> factory = scope.ServiceProvider.GetRequiredService<IMultiTenantDbContextFactory<IntegrationTestDbContext>>();
        IntegrationTestDbContext context = factory.CreateDbContext(tenantId);
        // For this factory, we seed data directly since tenant comes from claims
        await context.Database.EnsureCreatedAsync();
        await seedAction(context);
    }
}

// Factory para tests con resolver personalizado (Query string o custom)
public class CustomResolverMultiTenantWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public CustomResolverMultiTenantWebApplicationFactory() => _databaseName = $"Integration_Custom_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        builder.ConfigureServices(services =>
        {
            services.AddHttpContextAccessor();

            // Add controllers
            services.AddControllers();

            // Configure multi-tenant with discriminator strategy
            services.AddMultiTenantByDiscriminatorDbContext<IntegrationTestDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Use custom tenant resolver (query string based)
            services.AddCustomTenantResolver<QueryStringTenantResolver>();
        });

        builder.Configure(app =>
        {
            app.UseMultiTenant();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        });
    }

    public async Task SeedDataAsync(string tenantId, Func<IntegrationTestDbContext, Task> seedAction)
    {
        using IServiceScope scope = Services.CreateScope();

        Core.Abstractions.IWritableTenantStore? tenantStore = scope.ServiceProvider.GetRequiredService<Core.Abstractions.ITenantStore>() as IWritableTenantStore;
        tenantStore.SetCurrentTenantId(tenantId);
        IntegrationTestDbContext context = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();
        await context.Database.EnsureCreatedAsync();
        await seedAction(context);

        tenantStore.ClearCurrentTenantId();
    }
}

// Factory para tests sin multi-tenant
public class ExtensibleDbContextWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"Integration_Simple_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        builder.ConfigureServices(services =>
        {
            services.AddHttpContextAccessor();

            // Add controllers
            services.AddControllers();

            // Configure simple context with auditing
            services.AddExtensibleDbContext<SimpleDbContext>(
                options => options.UseInMemoryDatabase(_databaseName),
                features => features.EnableAuditing(config =>
                {
                    config.BehaviorWhenNoUser = AuditingBehavior.UseDefaultUser;
                    config.DefaultUserId = "SYSTEM";
                })
            );
        });

        builder.Configure(app =>
        {
            app.UseCurrentUser();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        });
    }
}

// DbContext simple para tests
public class SimpleDbContext : ExtensibleDbContext
{
    public SimpleDbContext(DbContextOptions<SimpleDbContext> options,
        IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<ProductAuditableWithUser> Products { get; set; } = null!;
}

// Entidad simple para tests
public class ProductAuditableWithUser : IAuditableWithUserEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;

    // IAuditableWithUserEntity
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

// DTO para tests
public class CreateProductAuditableWithUserRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class QueryStringTenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public QueryStringTenantResolver(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public Task<string?> ResolveTenantIdAsync()
    {
        HttpContext? context = _httpContextAccessor.HttpContext;
        if (context?.Request.Query.TryGetValue("tenant", out Microsoft.Extensions.Primitives.StringValues tenantValues) == true)
        {
            return Task.FromResult<string?>(tenantValues.FirstOrDefault());
        }

        return Task.FromResult<string?>(null);
    }
}

// Authentication handler para tests con claims
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    [Obsolete]
    public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if tenant-id is provided in headers for this test
        if (Context.Request.Headers.TryGetValue("X-Test-Tenant-Id", out Microsoft.Extensions.Primitives.StringValues tenantValues))
        {
            string? tenantId = tenantValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                Claim[] claims = new[]
                {
                    new Claim("tenant-id", tenantId),
                    new Claim(ClaimTypes.Name, "TestUser"),
                    new Claim(ClaimTypes.NameIdentifier, "test-user-id")
                };

                ClaimsIdentity identity = new(claims, "Test");
                ClaimsPrincipal principal = new(identity);
                AuthenticationTicket ticket = new(principal, "Test");

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}

// Extension methods para HttpClient
public static class HttpClientExtensions
{
    public static void SetTenantHeader(this HttpClient client, string tenantId)
    {
        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
    }

    public static void SetTestClaimsTenant(this HttpClient client, string tenantId)
    {
        client.DefaultRequestHeaders.Remove("X-Test-Tenant-Id");
        client.DefaultRequestHeaders.Add("X-Test-Tenant-Id", tenantId);
    }

    public static void ClearTenantHeaders(this HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Remove("X-Test-Tenant-Id");
    }
}

// Helper para crear URLs con query strings
public static class UrlHelper
{
    public static string WithTenantQuery(string baseUrl, string tenantId)
    {
        string separator = baseUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}{separator}tenant={tenantId}";
    }
}

// Helper para seeds de datos comunes
public static class TestDataSeeder
{
    public static async Task SeedBasicProductsAsync(IntegrationTestDbContext context)
    {
        TestProduct[] products = new[]
        {
            new TestProduct { Name = "Product 1", Price = 10.99m, Category = "Electronics" },
            new TestProduct { Name = "Product 2", Price = 25.50m, Category = "Books" },
            new TestProduct { Name = "Product 3", Price = 15.75m, Category = "Electronics" }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    public static async Task SeedOrdersAsync(IntegrationTestDbContext context)
    {
        List<TestProduct> products = await context.Products.ToListAsync();
        if (!products.Any()) return;

        TestOrder order = new()
        {
            CustomerName = "Test Customer",
            OrderDate = DateTime.UtcNow,
            Items = new List<TestOrderItem>
            {
                new() { Product = products[0], ProductId = products[0].Id, Quantity = 2, UnitPrice = products[0].Price },
                new() { Product = products[1], ProductId = products[1].Id, Quantity = 1, UnitPrice = products[1].Price }
            }
        };

        order.Total = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }
}