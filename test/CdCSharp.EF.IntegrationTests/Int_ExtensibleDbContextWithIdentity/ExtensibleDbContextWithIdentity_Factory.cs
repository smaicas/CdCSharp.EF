using CdCSharp.EF.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.IntegrationTests.Int_ExtensibleDbContextWithIdentity;

public class ExtensibleDbContextWithIdentity_Factory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"Integration_Identity_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        builder.ConfigureServices(services =>
        {
            services.AddHttpContextAccessor();

            // Add controllers
            services.AddControllers();

            // Configure extensible context with Identity
            services.AddExtensibleDbContext<ExtensibleDbContextWithIdentity_DbContext>(
                options => options.UseInMemoryDatabase(_databaseName),
                features => features.EnableIdentity<Guid>()
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
