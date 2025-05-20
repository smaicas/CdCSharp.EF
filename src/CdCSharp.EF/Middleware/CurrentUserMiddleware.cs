using CdCSharp.EF.Core.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.Middleware;

public class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        ICurrentUserStore userStore = context.RequestServices.GetRequiredService<ICurrentUserStore>();

        if (userStore is not IWritableCurrentUserStore writableStore)
        {
            await _next(context);
            return;
        }

        ICurrentUserResolver userResolver = context.RequestServices.GetRequiredService<ICurrentUserResolver>();

        try
        {
            string? userId = await userResolver.ResolveCurrentUserIdAsync();
            if (!string.IsNullOrEmpty(userId))
            {
                writableStore.SetCurrentUserId(userId);
            }

            await _next(context);
        }
        finally
        {
            writableStore.ClearCurrentUserId();
        }
    }
}
