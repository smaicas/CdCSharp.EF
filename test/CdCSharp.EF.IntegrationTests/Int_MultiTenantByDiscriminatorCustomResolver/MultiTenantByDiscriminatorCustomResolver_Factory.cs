using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDiscriminatorCustomResolver;

public class MultiTenantByDiscriminatorCustomResolver_Factory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public MultiTenantByDiscriminatorCustomResolver_Factory() => _databaseName = $"Integration_Custom_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        builder.ConfigureServices(services =>
        {
            services.AddHttpContextAccessor();

            // Add controllers
            services.AddControllers();

            // Configure multi-tenant with discriminator strategy
            services.AddMultiTenantByDiscriminatorDbContext<MultiTenantByDiscriminatorCustomResolver_DbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Use custom tenant resolver (query string based)
            services.AddCustomTenantResolver<MultiTenantByDiscriminatorCustomResolver_Resolver>();
        });

        builder.Configure(app =>
        {
            app.UseMultiTenant();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        });
    }

    public async Task SeedDataAsync(string tenantId, Func<MultiTenantByDiscriminatorCustomResolver_DbContext, Task> seedAction)
    {
        using IServiceScope scope = Services.CreateScope();

        IWritableTenantStore? tenantStore = scope.ServiceProvider.GetRequiredService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId(tenantId);
        MultiTenantByDiscriminatorCustomResolver_DbContext context =
            scope.ServiceProvider.GetRequiredService<MultiTenantByDiscriminatorCustomResolver_DbContext>();
        await context.Database.EnsureCreatedAsync();
        await seedAction(context);

        tenantStore!.ClearCurrentTenantId();
    }
}
