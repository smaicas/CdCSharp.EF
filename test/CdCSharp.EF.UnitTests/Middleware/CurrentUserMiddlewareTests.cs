using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CdCSharp.EF.UnitTests.Middleware;
public class CurrentUserMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithResolvedUser_SetsUserInStore()
    {
        // Arrange
        const string userId = "user123";
        Mock<ICurrentUserResolver> mockUserResolver = new();
        Mock<IWritableCurrentUserStore> mockUserStore = new();
        Mock<RequestDelegate> mockRequestDelegate = new();
        mockUserResolver.Setup(r => r.ResolveCurrentUserIdAsync()).ReturnsAsync(userId);
        ServiceCollection services = new();
        services.AddSingleton(mockUserResolver.Object);
        services.AddSingleton<ICurrentUserStore>(mockUserStore.Object);
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider
        };
        CurrentUserMiddleware middleware = new(mockRequestDelegate.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        mockUserResolver.Verify(r => r.ResolveCurrentUserIdAsync(), Times.Once);
        mockUserStore.Verify(s => s.SetCurrentUserId(userId), Times.Once);
        mockUserStore.Verify(s => s.ClearCurrentUserId(), Times.Once);
        mockRequestDelegate.Verify(d => d(httpContext), Times.Once);
        serviceProvider.Dispose();
    }

    [Fact]
    public async Task InvokeAsync_WithNullUser_DoesNotSetUserInStore()
    {
        // Arrange
        Mock<ICurrentUserResolver> mockUserResolver = new();
        Mock<IWritableCurrentUserStore> mockUserStore = new();
        Mock<RequestDelegate> mockRequestDelegate = new();
        mockUserResolver.Setup(r => r.ResolveCurrentUserIdAsync()).ReturnsAsync((string?)null);
        ServiceCollection services = new();
        services.AddSingleton(mockUserResolver.Object);
        services.AddSingleton<ICurrentUserStore>(mockUserStore.Object);
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider
        };
        CurrentUserMiddleware middleware = new(mockRequestDelegate.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        mockUserResolver.Verify(r => r.ResolveCurrentUserIdAsync(), Times.Once);
        mockUserStore.Verify(s => s.SetCurrentUserId(It.IsAny<string>()), Times.Never);
        mockUserStore.Verify(s => s.ClearCurrentUserId(), Times.Once);
        mockRequestDelegate.Verify(d => d(httpContext), Times.Once);
        serviceProvider.Dispose();
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyUser_DoesNotSetUserInStore()
    {
        // Arrange
        Mock<ICurrentUserResolver> mockUserResolver = new();
        Mock<IWritableCurrentUserStore> mockUserStore = new();
        Mock<RequestDelegate> mockRequestDelegate = new();
        mockUserResolver.Setup(r => r.ResolveCurrentUserIdAsync()).ReturnsAsync(string.Empty);
        ServiceCollection services = new();
        services.AddSingleton(mockUserResolver.Object);
        services.AddSingleton<ICurrentUserStore>(mockUserStore.Object);
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider
        };
        CurrentUserMiddleware middleware = new(mockRequestDelegate.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        mockUserResolver.Verify(r => r.ResolveCurrentUserIdAsync(), Times.Once);
        mockUserStore.Verify(s => s.SetCurrentUserId(It.IsAny<string>()), Times.Never);
        mockUserStore.Verify(s => s.ClearCurrentUserId(), Times.Once);
        mockRequestDelegate.Verify(d => d(httpContext), Times.Once);
        serviceProvider.Dispose();
    }

    [Fact]
    public async Task InvokeAsync_WhenResolverThrowsException_ClearsUserAndRethrows()
    {
        // Arrange
        InvalidOperationException expectedException = new("Resolver error");
        Mock<ICurrentUserResolver> mockUserResolver = new();
        Mock<IWritableCurrentUserStore> mockUserStore = new();
        Mock<RequestDelegate> mockRequestDelegate = new();
        mockUserResolver.Setup(r => r.ResolveCurrentUserIdAsync()).ThrowsAsync(expectedException);
        ServiceCollection services = new();
        services.AddSingleton(mockUserResolver.Object);
        services.AddSingleton<ICurrentUserStore>(mockUserStore.Object);
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider
        };
        CurrentUserMiddleware middleware = new(mockRequestDelegate.Object);

        // Act & Assert
        InvalidOperationException actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(httpContext));
        Assert.Same(expectedException, actualException);
        mockUserStore.Verify(s => s.ClearCurrentUserId(), Times.Once);
        mockRequestDelegate.Verify(d => d(It.IsAny<HttpContext>()), Times.Never);
        serviceProvider.Dispose();
    }

    [Fact]
    public async Task InvokeAsync_WhenNextMiddlewareThrowsException_ClearsUserAndRethrows()
    {
        // Arrange
        const string userId = "user123";
        InvalidOperationException expectedException = new("Next middleware error");
        Mock<ICurrentUserResolver> mockUserResolver = new();
        Mock<IWritableCurrentUserStore> mockUserStore = new();
        Mock<RequestDelegate> mockRequestDelegate = new();
        mockUserResolver.Setup(r => r.ResolveCurrentUserIdAsync()).ReturnsAsync(userId);
        mockRequestDelegate.Setup(d => d(It.IsAny<HttpContext>())).ThrowsAsync(expectedException);
        ServiceCollection services = new();
        services.AddSingleton(mockUserResolver.Object);
        services.AddSingleton<ICurrentUserStore>(mockUserStore.Object);
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider
        };
        CurrentUserMiddleware middleware = new(mockRequestDelegate.Object);

        // Act & Assert
        InvalidOperationException actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(httpContext));
        Assert.Same(expectedException, actualException);
        mockUserStore.Verify(s => s.SetCurrentUserId(userId), Times.Once);
        mockUserStore.Verify(s => s.ClearCurrentUserId(), Times.Once);
        serviceProvider.Dispose();
    }

    [Fact]
    public async Task InvokeAsync_ExecutesInCorrectOrder()
    {
        // Arrange
        const string userId = "user123";
        Mock<ICurrentUserResolver> mockUserResolver = new();
        Mock<IWritableCurrentUserStore> mockUserStore = new();
        Mock<RequestDelegate> mockRequestDelegate = new();
        List<string> callOrder = new();

        mockUserResolver.Setup(r => r.ResolveCurrentUserIdAsync())
            .Callback(() => callOrder.Add("ResolveUser"))
            .ReturnsAsync(userId);

        mockUserStore.Setup(s => s.SetCurrentUserId(It.IsAny<string>()))
            .Callback<string>(_ => callOrder.Add("SetUser"));

        mockRequestDelegate.Setup(d => d(It.IsAny<HttpContext>()))
            .Callback<HttpContext>(_ => callOrder.Add("NextMiddleware"))
            .Returns(Task.CompletedTask);

        mockUserStore.Setup(s => s.ClearCurrentUserId())
            .Callback(() => callOrder.Add("ClearUser"));

        ServiceCollection services = new();
        services.AddSingleton(mockUserResolver.Object);
        services.AddSingleton<ICurrentUserStore>(mockUserStore.Object);
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider
        };
        CurrentUserMiddleware middleware = new(mockRequestDelegate.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.Equal(new[] { "ResolveUser", "SetUser", "NextMiddleware", "ClearUser" }, callOrder);
        serviceProvider.Dispose();
    }
}
