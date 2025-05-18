using CdCSharp.EF.Core.Abstractions;

namespace CdCSharp.EF.Core.Stores;

public class InMemoryTenantStore : IWritableTenantStore
{
    private readonly AsyncLocal<string?> _tenantId = new();

    public string? GetCurrentTenantId() => _tenantId.Value;

    public void SetCurrentTenantId(string tenantId) => _tenantId.Value = tenantId;

    public void ClearCurrentTenantId() => _tenantId.Value = null;
}
