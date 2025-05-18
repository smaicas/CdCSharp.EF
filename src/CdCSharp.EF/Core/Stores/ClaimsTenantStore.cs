using CdCSharp.EF.Core.Abstractions;
using Microsoft.AspNetCore.Http;

namespace CdCSharp.EF.Core.Stores;

public class ClaimsTenantStore : ITenantStore
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsTenantStore(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public string? GetCurrentTenantId()
    {
        System.Security.Claims.ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirst("tenant-id")?.Value;
    }
}
