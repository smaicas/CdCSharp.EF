using CdCSharp.EF.Core.Stores;

namespace CdCSharp.EF.UnitTests.Core.Stores;

public class InMemoryCurrentUserStoreTests
{
    [Fact]
    public void GetCurrentUserId_WhenNotSet_ReturnsNull()
    {
        // Arrange
        InMemoryCurrentUserStore store = new();

        // Act
        string? result = store.GetCurrentUserId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SetCurrentUserId_WhenCalled_StoresUserId()
    {
        // Arrange
        InMemoryCurrentUserStore store = new();
        const string userId = "user123";

        // Act
        store.SetCurrentUserId(userId);
        string? result = store.GetCurrentUserId();

        // Assert
        Assert.Equal(userId, result);
    }

    [Fact]
    public void ClearCurrentUserId_WhenCalled_ClearsUserId()
    {
        // Arrange
        InMemoryCurrentUserStore store = new();
        const string userId = "user123";
        store.SetCurrentUserId(userId);

        // Act
        store.ClearCurrentUserId();
        string? result = store.GetCurrentUserId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UserStore_AcrossAsyncLocalContexts_IsThreadSafe()
    {
        // Arrange
        InMemoryCurrentUserStore store = new();

        // Act & Assert
        Task<string?> task1 = Task.Run(() =>
        {
            store.SetCurrentUserId("user1");
            Thread.Sleep(100);
            return store.GetCurrentUserId();
        });

        Task<string?> task2 = Task.Run(() =>
        {
            store.SetCurrentUserId("user2");
            Thread.Sleep(100);
            return store.GetCurrentUserId();
        });

        string?[] results = await Task.WhenAll(task1, task2);

        Assert.Equal("user1", results[0]);
        Assert.Equal("user2", results[1]);
    }
}
