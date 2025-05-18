namespace CdCSharp.EF.Core.Abstractions;

// Readonly
public interface ITenantStore
{
    string? GetCurrentTenantId();
}

// Read/Write
public interface IWritableTenantStore : ITenantStore
{
    void SetCurrentTenantId(string tenantId);
    void ClearCurrentTenantId();
}
