using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDatabase;

public class MultiTenantByDatabase_Factory : WebApplicationFactory<Program>, IDisposable
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

            // Configure multi-tenant with database strategy
            services.AddMultiTenantByDatabaseDbContext<MultiTenantByDatabase_DbContext>(
                tenants => tenants
                    .AddTenant("tenant1", options =>
                        options.UseInMemoryDatabase($"Integration_DB_tenant1_{_instanceId}"))
                    .AddTenant("tenant2", options =>
                        options.UseInMemoryDatabase($"Integration_DB_tenant2_{_instanceId}"))
                    .AddTenant("tenant3", options =>
                        options.UseInMemoryDatabase($"Integration_DB_tenant3_{_instanceId}"))
            );
        });

        builder.Configure(app =>
        {
            app.UseMultiTenant();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        });
    }

    public async Task SeedDataForTenantAsync(string tenantId, Func<MultiTenantByDatabase_DbContext, Task> seedAction)
    {
        using IServiceScope scope = Services.CreateScope();
        IMultiTenantDbContextFactory<MultiTenantByDatabase_DbContext> factory =
            scope.ServiceProvider.GetRequiredService<IMultiTenantDbContextFactory<MultiTenantByDatabase_DbContext>>();

        IWritableTenantStore? tenantStore =
            scope.ServiceProvider.GetRequiredService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId(tenantId);

        using MultiTenantByDatabase_DbContext context = factory.CreateDbContext(tenantId);
        await context.Database.EnsureCreatedAsync();
        await seedAction(context);
    }
}
