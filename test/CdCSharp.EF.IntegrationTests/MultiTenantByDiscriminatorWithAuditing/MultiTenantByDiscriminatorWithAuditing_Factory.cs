using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Extensions;
using CdCSharp.EF.Features.Auditing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.IntegrationTests.MultiTenantByDiscriminatorWithAuditing;

public class MultiTenantByDiscriminatorWithAuditing_Factory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public MultiTenantByDiscriminatorWithAuditing_Factory() =>
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
            services.AddMultiTenantByDiscriminatorDbContext<MultiTenantByDiscriminatorWithAuditing_DbContext>(
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

    public async Task SeedDataAsync(string tenantId, Func<MultiTenantByDiscriminatorWithAuditing_DbContext, Task> seedAction)
    {
        using IServiceScope scope = Services.CreateScope();

        IWritableTenantStore? tenantStore = scope.ServiceProvider.GetRequiredService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId(tenantId);

        MultiTenantByDiscriminatorWithAuditing_DbContext context =
            scope.ServiceProvider.GetRequiredService<MultiTenantByDiscriminatorWithAuditing_DbContext>();
        await context.Database.EnsureCreatedAsync();
        await seedAction(context);

        tenantStore!.ClearCurrentTenantId();
    }
}
