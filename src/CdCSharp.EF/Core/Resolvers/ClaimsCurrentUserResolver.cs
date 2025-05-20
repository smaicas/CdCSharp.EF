using CdCSharp.EF.Core.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CdCSharp.EF.Core.Resolvers;

public class ClaimsCurrentUserResolver : ICurrentUserResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _claimType;

    public ClaimsCurrentUserResolver(IHttpContextAccessor httpContextAccessor, string claimType = ClaimTypes.NameIdentifier)
    {
        _httpContextAccessor = httpContextAccessor;
        _claimType = claimType;
    }

    public Task<string?> ResolveCurrentUserIdAsync()
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        string? userId = user?.FindFirst(_claimType)?.Value;
        return Task.FromResult(userId);
    }
}
