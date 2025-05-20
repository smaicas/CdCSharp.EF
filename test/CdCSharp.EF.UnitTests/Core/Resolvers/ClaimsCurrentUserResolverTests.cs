using CdCSharp.EF.Core.Resolvers;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace CdCSharp.EF.UnitTests.Core.Resolvers;

public class ClaimsCurrentUserResolverTests
{
    [Fact]
    public async Task ResolveCurrentUserIdAsync_WhenClaimExists_ReturnsUserId()
    {
        // Arrange
        const string expectedUserId = "user123";

        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();
        ClaimsPrincipal mockUser = new(new ClaimsIdentity(new[]
        {
        new Claim(ClaimTypes.NameIdentifier, expectedUserId),
        new Claim(ClaimTypes.Name, "Test User")
        }));

        mockHttpContext.Setup(c => c.User).Returns(mockUser);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        ClaimsCurrentUserResolver resolver = new(mockHttpContextAccessor.Object);

        // Act
        string? result = await resolver.ResolveCurrentUserIdAsync();

        // Assert
        Assert.Equal(expectedUserId, result);
    }

    [Fact]
    public async Task ResolveCurrentUserIdAsync_WhenClaimDoesNotExist_ReturnsNull()
    {
        // Arrange
        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();
        ClaimsPrincipal mockUser = new(new ClaimsIdentity(new[]
        {
        new Claim(ClaimTypes.Name, "Test User"),
        new Claim(ClaimTypes.Email, "test@example.com")
        }));

        mockHttpContext.Setup(c => c.User).Returns(mockUser);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        ClaimsCurrentUserResolver resolver = new(mockHttpContextAccessor.Object);

        // Act
        string? result = await resolver.ResolveCurrentUserIdAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveCurrentUserIdAsync_WhenUserIsNull_ReturnsNull()
    {
        // Arrange
        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();

        mockHttpContext.Setup(c => c.User).Returns((ClaimsPrincipal?)null);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        ClaimsCurrentUserResolver resolver = new(mockHttpContextAccessor.Object);

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

        ClaimsCurrentUserResolver resolver = new(mockHttpContextAccessor.Object);

        // Act
        string? result = await resolver.ResolveCurrentUserIdAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveCurrentUserIdAsync_WithCustomClaimType_UsesCustomClaim()
    {
        // Arrange
        const string expectedUserId = "user123";
        const string customClaimType = "custom:user-id";

        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();
        ClaimsPrincipal mockUser = new(new ClaimsIdentity(new[]
        {
        new Claim(customClaimType, expectedUserId),
        new Claim(ClaimTypes.NameIdentifier, "different-id"),
        new Claim(ClaimTypes.Name, "Test User")
        }));

        mockHttpContext.Setup(c => c.User).Returns(mockUser);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        ClaimsCurrentUserResolver resolver = new(mockHttpContextAccessor.Object, customClaimType);

        // Act
        string? result = await resolver.ResolveCurrentUserIdAsync();

        // Assert
        Assert.Equal(expectedUserId, result);
    }

    [Fact]
    public async Task ResolveCurrentUserIdAsync_WithEmptyClaimValue_ReturnsEmptyString()
    {
        // Arrange
        const string emptyUserId = "";

        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();
        ClaimsPrincipal mockUser = new(new ClaimsIdentity(new[]
        {
        new Claim(ClaimTypes.NameIdentifier, emptyUserId)
        }));

        mockHttpContext.Setup(c => c.User).Returns(mockUser);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        ClaimsCurrentUserResolver resolver = new(mockHttpContextAccessor.Object);

        // Act
        string? result = await resolver.ResolveCurrentUserIdAsync();

        // Assert
        Assert.Equal(emptyUserId, result);
    }
}
