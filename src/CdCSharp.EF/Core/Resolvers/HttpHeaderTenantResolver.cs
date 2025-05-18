using CdCSharp.EF.Core.Abstractions;
using Microsoft.AspNetCore.Http;

namespace CdCSharp.EF.Core.Resolvers;

public class HttpHeaderTenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _headerName;

    public HttpHeaderTenantResolver(IHttpContextAccessor httpContextAccessor, string headerName = "X-Tenant-Id")
    {
        _httpContextAccessor = httpContextAccessor;
        _headerName = headerName;
    }

    public Task<string?> ResolveTenantIdAsync()
    {
        HttpContext context = _httpContextAccessor.HttpContext;
        if (context?.Request.Headers.TryGetValue(_headerName, out Microsoft.Extensions.Primitives.StringValues tenantId) == true)
        {
            return Task.FromResult<string?>(tenantId.FirstOrDefault());
        }

        return Task.FromResult<string?>(null);
    }
}
