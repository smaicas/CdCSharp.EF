using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Core.Stores;
using CdCSharp.EF.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CdCSharp.EF.UnitTests.Extensions;

public class ApplicationBuilderExtensionsTests
{
    [Fact]
    public void UseMultiTenant_WithWritableTenantStore_ReturnsApplicationBuilder()
    {
        // Arrange
        Mock<IApplicationBuilder> mockAppBuilder = new();
        Mock<IServiceProvider> mockApplicationServices = new();
        Mock<IServiceScope> mockScope = new();
        Mock<IServiceProvider> mockScopeServiceProvider = new();
        Mock<IServiceScopeFactory> mockScopeFactory = new();
        Mock<IWritableTenantStore> mockTenantStore = new();

        // Setup service provider chain
        mockApplicationServices.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(ITenantStore)))
            .Returns(mockTenantStore.Object);

        mockAppBuilder.Setup(ab => ab.ApplicationServices).Returns(mockApplicationServices.Object);

        // Setup UseMiddleware to return the same builder (fluent interface)
        mockAppBuilder.Setup(ab => ab.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Returns(mockAppBuilder.Object);

        // Act
        IApplicationBuilder result = mockAppBuilder.Object.UseMultiTenant();

        // Assert
        Assert.Same(mockAppBuilder.Object, result);
    }

    [Fact]
    public void UseMultiTenant_WithReadOnlyTenantStore_ReturnsApplicationBuilder()
    {
        // Arrange
        Mock<IApplicationBuilder> mockAppBuilder = new();
        Mock<IServiceProvider> mockApplicationServices = new();
        Mock<IServiceScope> mockScope = new();
        Mock<IServiceProvider> mockScopeServiceProvider = new();
        Mock<IServiceScopeFactory> mockScopeFactory = new();
        Mock<ITenantStore> mockTenantStore = new(); // Not IWritableTenantStore

        mockApplicationServices.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(ITenantStore)))
            .Returns(mockTenantStore.Object);

        mockAppBuilder.Setup(ab => ab.ApplicationServices).Returns(mockApplicationServices.Object);

        // Act
        IApplicationBuilder result = mockAppBuilder.Object.UseMultiTenant();

        // Assert
        Assert.Same(mockAppBuilder.Object, result);
    }

    [Fact]
    public void UseCurrentUser_WithWritableCurrentUserStore_ReturnsApplicationBuilder()
    {
        // Arrange
        Mock<IApplicationBuilder> mockAppBuilder = new();
        Mock<IServiceProvider> mockApplicationServices = new();
        Mock<IServiceScope> mockScope = new();
        Mock<IServiceProvider> mockScopeServiceProvider = new();
        Mock<IServiceScopeFactory> mockScopeFactory = new();
        Mock<IWritableCurrentUserStore> mockUserStore = new();

        mockApplicationServices.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(ICurrentUserStore)))
            .Returns(mockUserStore.Object);

        mockAppBuilder.Setup(ab => ab.ApplicationServices).Returns(mockApplicationServices.Object);

        // Setup UseMiddleware to return the same builder (fluent interface)
        mockAppBuilder.Setup(ab => ab.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Returns(mockAppBuilder.Object);

        // Act
        IApplicationBuilder result = mockAppBuilder.Object.UseCurrentUser();

        // Assert
        Assert.Same(mockAppBuilder.Object, result);
    }

    [Fact]
    public void UseCurrentUser_WithReadOnlyCurrentUserStore_ReturnsApplicationBuilder()
    {
        // Arrange
        Mock<IApplicationBuilder> mockAppBuilder = new();
        Mock<IServiceProvider> mockApplicationServices = new();
        Mock<IServiceScope> mockScope = new();
        Mock<IServiceProvider> mockScopeServiceProvider = new();
        Mock<IServiceScopeFactory> mockScopeFactory = new();
        Mock<ICurrentUserStore> mockUserStore = new(); // Not IWritableCurrentUserStore

        mockApplicationServices.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(ICurrentUserStore)))
            .Returns(mockUserStore.Object);

        mockAppBuilder.Setup(ab => ab.ApplicationServices).Returns(mockApplicationServices.Object);

        // Act
        IApplicationBuilder result = mockAppBuilder.Object.UseCurrentUser();

        // Assert
        Assert.Same(mockAppBuilder.Object, result);
    }

    [Fact]
    public void UseCurrentUser_WithNullUserStore_ReturnsApplicationBuilder()
    {
        // Arrange
        Mock<IApplicationBuilder> mockAppBuilder = new();
        Mock<IServiceProvider> mockApplicationServices = new();
        Mock<IServiceScope> mockScope = new();
        Mock<IServiceProvider> mockScopeServiceProvider = new();
        Mock<IServiceScopeFactory> mockScopeFactory = new();

        mockApplicationServices.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(ICurrentUserStore)))
            .Returns((ICurrentUserStore?)null);

        mockAppBuilder.Setup(ab => ab.ApplicationServices).Returns(mockApplicationServices.Object);

        // Act
        IApplicationBuilder result = mockAppBuilder.Object.UseCurrentUser();

        // Assert
        Assert.Same(mockAppBuilder.Object, result);
    }

    [Fact]
    public void UseMultiTenant_WithNullTenantStore_ThrowsException()
    {
        // Arrange
        Mock<IApplicationBuilder> mockAppBuilder = new();
        Mock<IServiceProvider> mockApplicationServices = new();
        Mock<IServiceScope> mockScope = new();
        Mock<IServiceProvider> mockScopeServiceProvider = new();
        Mock<IServiceScopeFactory> mockScopeFactory = new();

        mockApplicationServices.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
        // Setup GetService to return null, but GetRequiredService will throw
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(ITenantStore)))
            .Returns((ITenantStore?)null);

        mockAppBuilder.Setup(ab => ab.ApplicationServices).Returns(mockApplicationServices.Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => mockAppBuilder.Object.UseMultiTenant());
    }

    [Fact]
    public void UseMultiTenant_DisposesScope()
    {
        // Arrange
        Mock<IApplicationBuilder> mockAppBuilder = new();
        Mock<IServiceProvider> mockApplicationServices = new();
        Mock<IServiceScope> mockScope = new();
        Mock<IServiceProvider> mockScopeServiceProvider = new();
        Mock<IServiceScopeFactory> mockScopeFactory = new();
        Mock<ITenantStore> mockTenantStore = new();

        mockApplicationServices.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(ITenantStore)))
            .Returns(mockTenantStore.Object);

        mockAppBuilder.Setup(ab => ab.ApplicationServices).Returns(mockApplicationServices.Object);

        // Act
        mockAppBuilder.Object.UseMultiTenant();

        // Assert
        mockScope.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public void UseCurrentUser_DisposesScope()
    {
        // Arrange
        Mock<IApplicationBuilder> mockAppBuilder = new();
        Mock<IServiceProvider> mockApplicationServices = new();
        Mock<IServiceScope> mockScope = new();
        Mock<IServiceProvider> mockScopeServiceProvider = new();
        Mock<IServiceScopeFactory> mockScopeFactory = new();
        Mock<ICurrentUserStore> mockUserStore = new();

        mockApplicationServices.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(ICurrentUserStore)))
            .Returns(mockUserStore.Object);

        mockAppBuilder.Setup(ab => ab.ApplicationServices).Returns(mockApplicationServices.Object);

        // Act
        mockAppBuilder.Object.UseCurrentUser();

        // Assert
        mockScope.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public void UseMultiTenant_WithWritableTenantStore_CallsUseMiddleware()
    {
        // Arrange
        Mock<IApplicationBuilder> mockAppBuilder = new();
        Mock<IServiceProvider> mockApplicationServices = new();
        Mock<IServiceScope> mockScope = new();
        Mock<IServiceProvider> mockScopeServiceProvider = new();
        Mock<IServiceScopeFactory> mockScopeFactory = new();
        InMemoryTenantStore tenantStore = new(); // This is IWritableTenantStore

        mockApplicationServices.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(ITenantStore)))
            .Returns(tenantStore);

        mockAppBuilder.Setup(ab => ab.ApplicationServices).Returns(mockApplicationServices.Object);
        mockAppBuilder.Setup(ab => ab.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Returns(mockAppBuilder.Object);

        // Act
        IApplicationBuilder result = mockAppBuilder.Object.UseMultiTenant();

        // Assert
        Assert.Same(mockAppBuilder.Object, result);

        // Verify that UseMiddleware was called (which calls Use internally)
        mockAppBuilder.Verify(ab => ab.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
    }

    [Fact]
    public void UseCurrentUser_WithWritableCurrentUserStore_CallsUseMiddleware()
    {
        // Arrange
        Mock<IApplicationBuilder> mockAppBuilder = new();
        Mock<IServiceProvider> mockApplicationServices = new();
        Mock<IServiceScope> mockScope = new();
        Mock<IServiceProvider> mockScopeServiceProvider = new();
        Mock<IServiceScopeFactory> mockScopeFactory = new();
        InMemoryCurrentUserStore userStore = new(); // This is IWritableCurrentUserStore

        mockApplicationServices.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(ICurrentUserStore)))
            .Returns(userStore);

        mockAppBuilder.Setup(ab => ab.ApplicationServices).Returns(mockApplicationServices.Object);
        mockAppBuilder.Setup(ab => ab.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Returns(mockAppBuilder.Object);

        // Act
        IApplicationBuilder result = mockAppBuilder.Object.UseCurrentUser();

        // Assert
        Assert.Same(mockAppBuilder.Object, result);

        // Verify that UseMiddleware was called (which calls Use internally)
        mockAppBuilder.Verify(ab => ab.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
    }
}