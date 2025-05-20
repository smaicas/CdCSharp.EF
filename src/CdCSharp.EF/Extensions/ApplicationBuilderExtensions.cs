using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseMultiTenant(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        ITenantStore store = scope.ServiceProvider.GetRequiredService<ITenantStore>();
        if (store is IWritableTenantStore)
        {
            app = app.UseMiddleware<MultiTenantMiddleware>();
        }

        return app;
    }

    public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        ICurrentUserStore? userStore = scope.ServiceProvider.GetService<ICurrentUserStore>();
        if (userStore is IWritableCurrentUserStore)
        {
            app = app.UseMiddleware<CurrentUserMiddleware>();
        }

        return app;
    }
}
