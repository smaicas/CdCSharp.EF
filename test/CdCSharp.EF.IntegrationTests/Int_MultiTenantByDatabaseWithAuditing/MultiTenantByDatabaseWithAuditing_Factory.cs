using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Extensions;
using CdCSharp.EF.Features.Auditing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDatabaseWithAuditing;

public class MultiTenantByDatabaseWithAuditing_Factory : WebApplicationFactory<Program>
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

            services.AddExtensibleDbContext<MultiTenantByDatabaseWithAuditing_DbContext>(
                features => features
                .EnableMultiTenantByDatabase(
                    tenants => tenants
                    .AddTenant("tenant1", options => options.UseInMemoryDatabase($"Integration_DB_Audit_tenant1_{_instanceId}"))
                    .AddTenant("tenant2", options => options.UseInMemoryDatabase($"Integration_DB_Audit_tenant2_{_instanceId}"))
                    .AddTenant("tenant3", options => options.UseInMemoryDatabase($"Integration_DB_Audit_tenant3_{_instanceId}"))
                    )
                .EnableAuditing(config =>
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

    public async Task SeedDataForTenantAsync(string tenantId, Func<MultiTenantByDatabaseWithAuditing_DbContext, Task> seedAction)
    {
        using IServiceScope scope = Services.CreateScope();
        IMultiTenantDbContextFactory<MultiTenantByDatabaseWithAuditing_DbContext> factory =
            scope.ServiceProvider.GetRequiredService<IMultiTenantDbContextFactory<MultiTenantByDatabaseWithAuditing_DbContext>>();

        IWritableTenantStore? tenantStore =
            scope.ServiceProvider.GetRequiredService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId(tenantId);

        using MultiTenantByDatabaseWithAuditing_DbContext context = factory.CreateDbContext(tenantId);
        await context.Database.EnsureCreatedAsync();
        await seedAction(context);
    }
}
