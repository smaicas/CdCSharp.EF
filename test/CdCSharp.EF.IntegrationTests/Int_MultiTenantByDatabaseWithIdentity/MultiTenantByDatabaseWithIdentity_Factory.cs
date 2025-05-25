using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDatabaseWithIdentity;

public class MultiTenantByDatabaseWithIdentity_Factory : WebApplicationFactory<Program>
{
    private readonly string _instanceId = Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        builder.ConfigureServices(services =>
        {
            services.AddHttpContextAccessor();
            services.AddControllers();

            // Configure multi-tenant with database strategy and Identity
            services.AddExtensibleDbContext<MultiTenantByDatabaseWithIdentity_DbContext>(
                features =>
                {
                    features.EnableMultiTenantByDatabase(tenants => tenants
                        .AddTenant("tenant1", options =>
                            options.UseInMemoryDatabase($"Integration_DB_Identity_tenant1_{_instanceId}"))
                        .AddTenant("tenant2", options =>
                            options.UseInMemoryDatabase($"Integration_DB_Identity_tenant2_{_instanceId}"))
                        .AddTenant("tenant3", options =>
                            options.UseInMemoryDatabase($"Integration_DB_Identity_tenant3_{_instanceId}")));

                    features.EnableIdentity<Guid>();
                    return features;
                }
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

    public async Task SeedDataForTenantAsync(string tenantId, Func<MultiTenantByDatabaseWithIdentity_DbContext, Task> seedAction)
    {
        using IServiceScope scope = Services.CreateScope();
        IMultiTenantDbContextFactory<MultiTenantByDatabaseWithIdentity_DbContext> factory =
            scope.ServiceProvider.GetRequiredService<IMultiTenantDbContextFactory<MultiTenantByDatabaseWithIdentity_DbContext>>();

        IWritableTenantStore? tenantStore =
            scope.ServiceProvider.GetRequiredService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId(tenantId);

        using MultiTenantByDatabaseWithIdentity_DbContext context = factory.CreateDbContext(tenantId);
        await context.Database.EnsureCreatedAsync();
        await seedAction(context);
    }
}
