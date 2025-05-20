using CdCSharp.EF.Core.Abstractions;

namespace CdCSharp.EF.Core.Stores;

public class InMemoryCurrentUserStore : IWritableCurrentUserStore
{
    private readonly AsyncLocal<string?> _userId = new();

    public string? GetCurrentUserId() => _userId.Value;

    public void SetCurrentUserId(string userId) => _userId.Value = userId;

    public void ClearCurrentUserId() => _userId.Value = null;
}
