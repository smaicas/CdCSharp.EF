using CdCSharp.EF.Core.Abstractions;
using Microsoft.AspNetCore.Http;

namespace CdCSharp.EF.Core.Resolvers;

public class HttpHeaderCurrentUserResolver : ICurrentUserResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _headerName;

    public HttpHeaderCurrentUserResolver(IHttpContextAccessor httpContextAccessor, string headerName = "X-User-Id")
    {
        _httpContextAccessor = httpContextAccessor;
        _headerName = headerName;
    }

    public Task<string?> ResolveCurrentUserIdAsync()
    {
        HttpContext context = _httpContextAccessor.HttpContext;
        if (context?.Request.Headers.TryGetValue(_headerName, out Microsoft.Extensions.Primitives.StringValues userId) == true)
        {
            return Task.FromResult<string?>(userId.FirstOrDefault());
        }

        return Task.FromResult<string?>(null);
    }
}
