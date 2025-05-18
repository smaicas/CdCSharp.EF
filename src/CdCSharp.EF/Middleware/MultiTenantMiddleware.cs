using CdCSharp.EF.Core.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.Middleware;

public class MultiTenantMiddleware
{
    private readonly RequestDelegate _next;

    public MultiTenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        ITenantStore tenantStore = context.RequestServices.GetRequiredService<ITenantStore>();

        if (tenantStore is not IWritableTenantStore writableStore)
        {
            await _next(context);
            return;
        }

        ITenantResolver tenantResolver = context.RequestServices.GetRequiredService<ITenantResolver>();

        try
        {
            string? tenantId = await tenantResolver.ResolveTenantIdAsync();
            if (!string.IsNullOrEmpty(tenantId))
            {
                writableStore.SetCurrentTenantId(tenantId);
            }

            await _next(context);
        }
        finally
        {
            writableStore.ClearCurrentTenantId();
        }
    }
}
