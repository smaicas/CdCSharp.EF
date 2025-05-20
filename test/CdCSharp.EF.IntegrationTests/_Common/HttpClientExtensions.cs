namespace CdCSharp.EF.IntegrationTests._Common;

public static class HttpClientExtensions
{
    public static void SetTenantHeader(this HttpClient client, string tenantId)
    {
        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
    }

    public static void SetTestClaimsTenant(this HttpClient client, string tenantId)
    {
        client.DefaultRequestHeaders.Remove("X-Test-Tenant-Id");
        client.DefaultRequestHeaders.Add("X-Test-Tenant-Id", tenantId);
    }

    public static void ClearTenantHeaders(this HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Remove("X-Test-Tenant-Id");
    }
}
