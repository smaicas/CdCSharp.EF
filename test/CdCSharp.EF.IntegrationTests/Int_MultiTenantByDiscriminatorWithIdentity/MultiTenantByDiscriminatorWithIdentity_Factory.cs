using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDiscriminatorWithIdentity;

public class MultiTenantByDiscriminatorWithIdentity_Factory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public MultiTenantByDiscriminatorWithIdentity_Factory() =>
        _databaseName = $"Integration_DiscriminatorIdentity_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        builder.ConfigureServices(services =>
        {
            services.AddHttpContextAccessor();

            // Add controllers
            services.AddControllers();

            // Configure multi-tenant with discriminator strategy and Identity
            services.AddMultiTenantByDiscriminatorDbContext<MultiTenantByDiscriminatorWithIdentity_DbContext>(
                options => options.UseInMemoryDatabase(_databaseName),
                features => features.EnableIdentity<Guid>()
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

    public async Task SeedDataAsync(string tenantId, Func<MultiTenantByDiscriminatorWithIdentity_DbContext, Task> seedAction)
    {
        using IServiceScope scope = Services.CreateScope();

        IWritableTenantStore? tenantStore = scope.ServiceProvider.GetRequiredService<ITenantStore>() as IWritableTenantStore;
        tenantStore!.SetCurrentTenantId(tenantId);

        MultiTenantByDiscriminatorWithIdentity_DbContext context =
            scope.ServiceProvider.GetRequiredService<MultiTenantByDiscriminatorWithIdentity_DbContext>();
        await context.Database.EnsureCreatedAsync();
        await seedAction(context);

        tenantStore!.ClearCurrentTenantId();
    }
}
