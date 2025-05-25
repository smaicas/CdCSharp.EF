using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDiscriminator;

public class MultiTenantByDiscriminator_Factory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public MultiTenantByDiscriminator_Factory() => _databaseName = $"Integration_Discriminator_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        builder.ConfigureServices(services =>
        {
            services.AddHttpContextAccessor();

            // Add controllers
            services.AddControllers();

            // Configure multi-tenant with discriminator strategy
            services.AddExtensibleDbContext<MultiTenantByDiscriminator_DbContext>(
                features => features.EnableMultiTenantByDiscriminator(options =>
                options.UseInMemoryDatabase(_databaseName))
                );
        });

        builder.Configure(app =>
        {
            app.UseMultiTenant();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        });
    }

    public async Task SeedDataAsync(string tenantId, Func<MultiTenantByDiscriminator_DbContext, Task> seedAction)
    {
        using IServiceScope scope = Services.CreateScope();

        IWritableTenantStore? tenantStore = scope.ServiceProvider.GetRequiredService<ITenantStore>() as IWritableTenantStore;

        tenantStore!.SetCurrentTenantId(tenantId);
        MultiTenantByDiscriminator_DbContext context = scope.ServiceProvider.GetRequiredService<MultiTenantByDiscriminator_DbContext>();
        await context.Database.EnsureCreatedAsync();
        await seedAction(context);

        tenantStore!.ClearCurrentTenantId();
    }
}
