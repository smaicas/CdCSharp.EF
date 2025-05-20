using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CdCSharp.EF.UnitTests.Middleware;

public class MultiTenantMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithResolvedTenant_SetsTenantInStore()
    {
        // Arrange
        const string tenantId = "tenant1";
        Mock<ITenantResolver> mockTenantResolver = new();
        Mock<IWritableTenantStore> mockTenantStore = new();
        Mock<RequestDelegate> mockRequestDelegate = new();

        mockTenantResolver.Setup(r => r.ResolveTenantIdAsync()).ReturnsAsync(tenantId);

        ServiceCollection services = new();
        services.AddSingleton(mockTenantResolver.Object);
        services.AddSingleton<ITenantStore>(mockTenantStore.Object);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider
        };

        MultiTenantMiddleware middleware = new(mockRequestDelegate.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        mockTenantResolver.Verify(r => r.ResolveTenantIdAsync(), Times.Once);
        mockTenantStore.Verify(s => s.SetCurrentTenantId(tenantId), Times.Once);
        mockTenantStore.Verify(s => s.ClearCurrentTenantId(), Times.Once);
        mockRequestDelegate.Verify(d => d(httpContext), Times.Once);

        serviceProvider.Dispose();
    }

    [Fact]
    public async Task InvokeAsync_WithNullTenant_DoesNotSetTenantInStore()
    {
        // Arrange
        Mock<ITenantResolver> mockTenantResolver = new();
        Mock<IWritableTenantStore> mockTenantStore = new();
        Mock<RequestDelegate> mockRequestDelegate = new();

        mockTenantResolver.Setup(r => r.ResolveTenantIdAsync()).ReturnsAsync((string?)null);

        ServiceCollection services = new();
        services.AddSingleton(mockTenantResolver.Object);
        services.AddSingleton<ITenantStore>(mockTenantStore.Object);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider
        };

        MultiTenantMiddleware middleware = new(mockRequestDelegate.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        mockTenantResolver.Verify(r => r.ResolveTenantIdAsync(), Times.Once);
        mockTenantStore.Verify(s => s.SetCurrentTenantId(It.IsAny<string>()), Times.Never);
        mockTenantStore.Verify(s => s.ClearCurrentTenantId(), Times.Once);
        mockRequestDelegate.Verify(d => d(httpContext), Times.Once);

        serviceProvider.Dispose();
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyTenant_DoesNotSetTenantInStore()
    {
        // Arrange
        Mock<ITenantResolver> mockTenantResolver = new();
        Mock<IWritableTenantStore> mockTenantStore = new();
        Mock<RequestDelegate> mockRequestDelegate = new();

        mockTenantResolver.Setup(r => r.ResolveTenantIdAsync()).ReturnsAsync(string.Empty);

        ServiceCollection services = new();
        services.AddSingleton(mockTenantResolver.Object);
        services.AddSingleton<ITenantStore>(mockTenantStore.Object);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider
        };

        MultiTenantMiddleware middleware = new(mockRequestDelegate.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        mockTenantResolver.Verify(r => r.ResolveTenantIdAsync(), Times.Once);
        mockTenantStore.Verify(s => s.SetCurrentTenantId(It.IsAny<string>()), Times.Never);
        mockTenantStore.Verify(s => s.ClearCurrentTenantId(), Times.Once);
        mockRequestDelegate.Verify(d => d(httpContext), Times.Once);

        serviceProvider.Dispose();
    }

    [Fact]
    public async Task InvokeAsync_WhenResolverThrowsException_ClearsTenantAndRethrows()
    {
        // Arrange
        InvalidOperationException expectedException = new("Resolver error");
        Mock<ITenantResolver> mockTenantResolver = new();
        Mock<IWritableTenantStore> mockTenantStore = new();
        Mock<RequestDelegate> mockRequestDelegate = new();

        mockTenantResolver.Setup(r => r.ResolveTenantIdAsync()).ThrowsAsync(expectedException);

        ServiceCollection services = new();
        services.AddSingleton(mockTenantResolver.Object);
        services.AddSingleton<ITenantStore>(mockTenantStore.Object);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider
        };

        MultiTenantMiddleware middleware = new(mockRequestDelegate.Object);

        // Act & Assert
        InvalidOperationException actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(httpContext));

        Assert.Same(expectedException, actualException);
        mockTenantStore.Verify(s => s.ClearCurrentTenantId(), Times.Once);
        mockRequestDelegate.Verify(d => d(It.IsAny<HttpContext>()), Times.Never);

        serviceProvider.Dispose();
    }

    [Fact]
    public async Task InvokeAsync_WhenNextMiddlewareThrowsException_ClearsTenantAndRethrows()
    {
        // Arrange
        const string tenantId = "tenant1";
        InvalidOperationException expectedException = new("Next middleware error");
        Mock<ITenantResolver> mockTenantResolver = new();
        Mock<IWritableTenantStore> mockTenantStore = new();
        Mock<RequestDelegate> mockRequestDelegate = new();

        mockTenantResolver.Setup(r => r.ResolveTenantIdAsync()).ReturnsAsync(tenantId);
        mockRequestDelegate.Setup(d => d(It.IsAny<HttpContext>())).ThrowsAsync(expectedException);

        ServiceCollection services = new();
        services.AddSingleton(mockTenantResolver.Object);
        services.AddSingleton<ITenantStore>(mockTenantStore.Object);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider
        };

        MultiTenantMiddleware middleware = new(mockRequestDelegate.Object);

        // Act & Assert
        InvalidOperationException actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(httpContext));

        Assert.Same(expectedException, actualException);
        mockTenantStore.Verify(s => s.SetCurrentTenantId(tenantId), Times.Once);
        mockTenantStore.Verify(s => s.ClearCurrentTenantId(), Times.Once);

        serviceProvider.Dispose();
    }

    [Fact]
    public async Task InvokeAsync_ExecutesInCorrectOrder()
    {
        // Arrange
        const string tenantId = "tenant1";
        Mock<ITenantResolver> mockTenantResolver = new();
        Mock<IWritableTenantStore> mockTenantStore = new();
        Mock<RequestDelegate> mockRequestDelegate = new();
        List<string> callOrder = new();

        mockTenantResolver.Setup(r => r.ResolveTenantIdAsync())
            .Callback(() => callOrder.Add("ResolveTenant"))
            .ReturnsAsync(tenantId);

        mockTenantStore.Setup(s => s.SetCurrentTenantId(It.IsAny<string>()))
            .Callback<string>(_ => callOrder.Add("SetTenant"));

        mockRequestDelegate.Setup(d => d(It.IsAny<HttpContext>()))
            .Callback<HttpContext>(_ => callOrder.Add("NextMiddleware"))
            .Returns(Task.CompletedTask);

        mockTenantStore.Setup(s => s.ClearCurrentTenantId())
            .Callback(() => callOrder.Add("ClearTenant"));

        ServiceCollection services = new();
        services.AddSingleton(mockTenantResolver.Object);
        services.AddSingleton<ITenantStore>(mockTenantStore.Object);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider
        };

        MultiTenantMiddleware middleware = new(mockRequestDelegate.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.Equal(new[] { "ResolveTenant", "SetTenant", "NextMiddleware", "ClearTenant" }, callOrder);

        serviceProvider.Dispose();
    }
}