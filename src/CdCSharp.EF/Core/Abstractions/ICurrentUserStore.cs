namespace CdCSharp.EF.Core.Abstractions;

// Readonly
public interface ICurrentUserStore
{
    string? GetCurrentUserId();
}

// Read/Write
public interface IWritableCurrentUserStore : ICurrentUserStore
{
    void SetCurrentUserId(string userId);
    void ClearCurrentUserId();
}
