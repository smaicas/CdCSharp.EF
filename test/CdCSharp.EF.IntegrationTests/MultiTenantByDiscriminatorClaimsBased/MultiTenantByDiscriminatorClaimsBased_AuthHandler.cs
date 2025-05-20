using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CdCSharp.EF.IntegrationTests.MultiTenantByDiscriminatorClaimsBased;

public class MultiTenantByDiscriminatorClaimsBased_AuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public MultiTenantByDiscriminatorClaimsBased_AuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) // Usar ITimeProvider en lugar de ISystemClock
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if tenant-id is provided in headers for this test
        if (Context.Request.Headers.TryGetValue("X-Test-Tenant-Id", out Microsoft.Extensions.Primitives.StringValues tenantValues))
        {
            string? tenantId = tenantValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                Claim[] claims = new[]
                {
                    new Claim("tenant-id", tenantId),
                    new Claim(ClaimTypes.Name, "TestUser"),
                    new Claim(ClaimTypes.NameIdentifier, "test-user-id")
                };

                ClaimsIdentity identity = new(claims, "Test");
                ClaimsPrincipal principal = new(identity);
                AuthenticationTicket ticket = new(principal, "Test");

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}