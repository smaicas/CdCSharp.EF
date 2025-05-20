using CdCSharp.EF.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.IntegrationTests.ExtensibleDbContext;

public class ExtensibleDbContext_Factory : WebApplicationFactory<Program>
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
            services.AddExtensibleDbContext<ExtensibleDbContext_DbContext>(
                options => options.UseInMemoryDatabase(_databaseName)
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
