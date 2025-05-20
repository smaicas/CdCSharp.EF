using CdCSharp.EF.Core.Resolvers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;

namespace CdCSharp.EF.UnitTests.Core.Resolvers;

public class HttpHeaderCurrentUserResolverTests
{
    [Fact]
    public async Task ResolveCurrentUserIdAsync_WhenHeaderExists_ReturnsUserId()
    {
        // Arrange
        const string expectedUserId = "user123";
        const string headerName = "X-User-Id";

        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();
        Mock<HttpRequest> mockRequest = new();
        Mock<IHeaderDictionary> mockHeaders = new();

        mockHeaders.Setup(h => h.TryGetValue(headerName, out It.Ref<StringValues>.IsAny))
            .Returns((string key, out StringValues value) =>
            {
                value = new StringValues(expectedUserId);
                return true;
            });

        mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        HttpHeaderCurrentUserResolver resolver = new(mockHttpContextAccessor.Object, headerName);

        // Act
        string? result = await resolver.ResolveCurrentUserIdAsync();

        // Assert
        Assert.Equal(expectedUserId, result);
    }

    [Fact]
    public async Task ResolveCurrentUserIdAsync_WhenHeaderDoesNotExist_ReturnsNull()
    {
        // Arrange
        const string headerName = "X-User-Id";

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

        HttpHeaderCurrentUserResolver resolver = new(mockHttpContextAccessor.Object, headerName);

        // Act
        string? result = await resolver.ResolveCurrentUserIdAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveCurrentUserIdAsync_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        HttpHeaderCurrentUserResolver resolver = new(mockHttpContextAccessor.Object);

        // Act
        string? result = await resolver.ResolveCurrentUserIdAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveCurrentUserIdAsync_WithCustomHeaderName_UsesCustomHeader()
    {
        // Arrange
        const string expectedUserId = "user123";
        const string customHeaderName = "Custom-User-Header";

        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();
        Mock<HttpRequest> mockRequest = new();
        Mock<IHeaderDictionary> mockHeaders = new();

        mockHeaders.Setup(h => h.TryGetValue(customHeaderName, out It.Ref<StringValues>.IsAny))
            .Returns((string key, out StringValues value) =>
            {
                value = new StringValues(expectedUserId);
                return true;
            });

        // Ensure default header name doesn't match
        mockHeaders.Setup(h => h.TryGetValue("X-User-Id", out It.Ref<StringValues>.IsAny))
            .Returns((string key, out StringValues value) =>
            {
                value = new StringValues("wrong-user");
                return true;
            });

        mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        HttpHeaderCurrentUserResolver resolver = new(mockHttpContextAccessor.Object, customHeaderName);

        // Act
        string? result = await resolver.ResolveCurrentUserIdAsync();

        // Assert
        Assert.Equal(expectedUserId, result);
    }

    [Fact]
    public async Task ResolveCurrentUserIdAsync_WithMultipleHeaderValues_ReturnsFirstValue()
    {
        // Arrange
        string[] headerValues = new[] { "user1", "user2", "user3" };
        const string headerName = "X-User-Id";

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

        HttpHeaderCurrentUserResolver resolver = new(mockHttpContextAccessor.Object, headerName);

        // Act
        string? result = await resolver.ResolveCurrentUserIdAsync();

        // Assert
        Assert.Equal("user1", result);
    }

    [Fact]
    public async Task ResolveCurrentUserIdAsync_WithEmptyHeaderValue_ReturnsEmptyString()
    {
        // Arrange
        const string headerName = "X-User-Id";

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

        HttpHeaderCurrentUserResolver resolver = new(mockHttpContextAccessor.Object, headerName);

        // Act
        string? result = await resolver.ResolveCurrentUserIdAsync();

        // Assert
        Assert.Equal(string.Empty, result);
    }
}
