namespace CdCSharp.EF.Core.Abstractions;

public interface ITenantResolver
{
    Task<string?> ResolveTenantIdAsync();
}
