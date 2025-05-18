using CdCSharp.EF.Core.Resolvers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;

namespace CdCSharp.EF.UnitTests;

public class HttpHeaderTenantResolverTests
{
    [Fact]
    public async Task ResolveTenantIdAsync_WhenHeaderExists_ReturnsTenantId()
    {
        // Arrange
        const string expectedTenantId = "tenant1";
        const string headerName = "X-Tenant-Id";

        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();
        Mock<HttpRequest> mockRequest = new();
        Mock<IHeaderDictionary> mockHeaders = new();

        mockHeaders.Setup(h => h.TryGetValue(headerName, out It.Ref<StringValues>.IsAny))
            .Returns((string key, out StringValues value) =>
            {
                value = new StringValues(expectedTenantId);
                return true;
            });

        mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        HttpHeaderTenantResolver resolver = new(mockHttpContextAccessor.Object, headerName);

        // Act
        string? result = await resolver.ResolveTenantIdAsync();

        // Assert
        Assert.Equal(expectedTenantId, result);
    }

    [Fact]
    public async Task ResolveTenantIdAsync_WhenHeaderDoesNotExist_ReturnsNull()
    {
        // Arrange
        const string headerName = "X-Tenant-Id";

        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();
        Mock<HttpRequest> mockRequest = new();
        Mock<IHeaderDictionary> mockHeaders = new();

        mockHeaders.Setup(h => h.TryGetValue(headerName, out It.Ref<StringValues>.IsAny))
            .Returns((string key, out StringValues value) =>
            {
                value = default;
                return false;
            });

        mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        HttpHeaderTenantResolver resolver = new(mockHttpContextAccessor.Object, headerName);

        // Act
        string? result = await resolver.ResolveTenantIdAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveTenantIdAsync_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        HttpHeaderTenantResolver resolver = new(mockHttpContextAccessor.Object);

        // Act
        string? result = await resolver.ResolveTenantIdAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveTenantIdAsync_WithCustomHeaderName_UseCustomHeader()
    {
        // Arrange
        const string expectedTenantId = "tenant1";
        const string customHeaderName = "Custom-Tenant-Header";

        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();
        Mock<HttpRequest> mockRequest = new();
        Mock<IHeaderDictionary> mockHeaders = new();

        mockHeaders.Setup(h => h.TryGetValue(customHeaderName, out It.Ref<StringValues>.IsAny))
            .Returns((string key, out StringValues value) =>
            {
                value = new StringValues(expectedTenantId);
                return true;
            });

        mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        HttpHeaderTenantResolver resolver = new(mockHttpContextAccessor.Object, customHeaderName);

        // Act
        string? result = await resolver.ResolveTenantIdAsync();

        // Assert
        Assert.Equal(expectedTenantId, result);
    }

    [Fact]
    public async Task ResolveTenantIdAsync_WithMultipleHeaderValues_ReturnsFirstValue()
    {
        // Arrange
        string[] headerValues = new[] { "tenant1", "tenant2", "tenant3" };
        const string headerName = "X-Tenant-Id";

        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();
        Mock<HttpRequest> mockRequest = new();
        Mock<IHeaderDictionary> mockHeaders = new();

        mockHeaders.Setup(h => h.TryGetValue(headerName, out It.Ref<StringValues>.IsAny))
            .Returns((string key, out StringValues value) =>
            {
                value = new StringValues(headerValues);
                return true;
            });

        mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        HttpHeaderTenantResolver resolver = new(mockHttpContextAccessor.Object, headerName);

        // Act
        string? result = await resolver.ResolveTenantIdAsync();

        // Assert
        Assert.Equal("tenant1", result);
    }

    [Fact]
    public async Task ResolveTenantIdAsync_WithEmptyHeaderValue_ReturnsNull()
    {
        // Arrange
        const string headerName = "X-Tenant-Id";

        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();
        Mock<HttpRequest> mockRequest = new();
        Mock<IHeaderDictionary> mockHeaders = new();

        mockHeaders.Setup(h => h.TryGetValue(headerName, out It.Ref<StringValues>.IsAny))
            .Returns((string key, out StringValues value) =>
            {
                value = new StringValues(string.Empty);
                return true;
            });

        mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        HttpHeaderTenantResolver resolver = new(mockHttpContextAccessor.Object, headerName);

        // Act
        string? result = await resolver.ResolveTenantIdAsync();

        // Assert
        Assert.Equal(string.Empty, result);
    }
}
