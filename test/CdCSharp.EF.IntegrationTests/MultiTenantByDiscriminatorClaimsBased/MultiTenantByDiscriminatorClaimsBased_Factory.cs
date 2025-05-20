using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.IntegrationTests.MultiTenantByDiscriminatorClaimsBased;

public class MultiTenantByDiscriminatorClaimsBased_Factory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public MultiTenantByDiscriminatorClaimsBased_Factory() => _databaseName = $"Integration_Claims_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        builder.ConfigureServices(services =>
        {
            services.AddHttpContextAccessor();

            // Add authentication for claims
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, MultiTenantByDiscriminatorClaimsBased_AuthHandler>(
                    "Test", options => { });

            // Add controllers
            services.AddControllers();

            // Configure multi-tenant with discriminator strategy
            services.AddMultiTenantByDiscriminatorDbContext<MultiTenantByDiscriminatorClaimsBased_DbContext>(options =>
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

    public async Task SeedDataAsync(string tenantId, Func<MultiTenantByDiscriminatorClaimsBased_DbContext, Task> seedAction)
    {
        using IServiceScope scope = Services.CreateScope();

        IMultiTenantDbContextFactory<MultiTenantByDiscriminatorClaimsBased_DbContext> factory =
            scope.ServiceProvider.GetRequiredService<IMultiTenantDbContextFactory<MultiTenantByDiscriminatorClaimsBased_DbContext>>();
        MultiTenantByDiscriminatorClaimsBased_DbContext context = factory.CreateDbContext(tenantId);

        // For this factory, we seed data directly since tenant comes from claims
        await context.Database.EnsureCreatedAsync();
        await seedAction(context);
    }
}
