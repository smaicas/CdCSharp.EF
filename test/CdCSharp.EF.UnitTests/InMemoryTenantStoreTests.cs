using CdCSharp.EF.Core.Stores;

namespace CdCSharp.EF.UnitTests;

public class InMemoryTenantStoreTests
{
    [Fact]
    public void GetCurrentTenantId_WhenNotSet_ReturnsNull()
    {
        // Arrange
        InMemoryTenantStore store = new();

        // Act
        string? result = store.GetCurrentTenantId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SetCurrentTenantId_WhenCalled_StoresTenantId()
    {
        // Arrange
        InMemoryTenantStore store = new();
        const string tenantId = "tenant1";

        // Act
        store.SetCurrentTenantId(tenantId);
        string? result = store.GetCurrentTenantId();

        // Assert
        Assert.Equal(tenantId, result);
    }

    [Fact]
    public void ClearCurrentTenantId_WhenCalled_ClearsTenantId()
    {
        // Arrange
        InMemoryTenantStore store = new();
        const string tenantId = "tenant1";
        store.SetCurrentTenantId(tenantId);

        // Act
        store.ClearCurrentTenantId();
        string? result = store.GetCurrentTenantId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SetCurrentTenantId_CalledMultipleTimes_StoresLatestValue()
    {
        // Arrange
        InMemoryTenantStore store = new();
        const string firstTenantId = "tenant1";
        const string secondTenantId = "tenant2";

        // Act
        store.SetCurrentTenantId(firstTenantId);
        store.SetCurrentTenantId(secondTenantId);
        string? result = store.GetCurrentTenantId();

        // Assert
        Assert.Equal(secondTenantId, result);
    }

    [Fact]
    public async Task TenantStore_IsThreadSafe_AcrossAsyncLocalContexts()
    {
        // Arrange
        InMemoryTenantStore store = new();

        // Act & Assert
        Task<string> task1 = Task.Run(() =>
        {
            store.SetCurrentTenantId("tenant1");
            Thread.Sleep(100);
            return store.GetCurrentTenantId();
        });

        Task<string> task2 = Task.Run(() =>
        {
            store.SetCurrentTenantId("tenant2");
            Thread.Sleep(100);
            return store.GetCurrentTenantId();
        });

        object[] results = await Task.WhenAll(task1, task2);

        Assert.Equal("tenant1", results[0]);
        Assert.Equal("tenant2", results[1]);
    }
}
