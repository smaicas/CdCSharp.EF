using CdCSharp.EF.Core.Stores;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace CdCSharp.EF.UnitTests;

public class ClaimsTenantStoreTests
{
    [Fact]
    public void GetCurrentTenantId_WhenTenantClaimExists_ReturnsTenantId()
    {
        // Arrange
        const string expectedTenantId = "tenant1";
        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();
        Mock<ClaimsPrincipal> mockUser = new();

        Claim tenantClaim = new("tenant-id", expectedTenantId);
        mockUser.Setup(u => u.FindFirst("tenant-id")).Returns(tenantClaim);
        mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        ClaimsTenantStore store = new(mockHttpContextAccessor.Object);

        // Act
        string? result = store.GetCurrentTenantId();

        // Assert
        Assert.Equal(expectedTenantId, result);
    }

    [Fact]
    public void GetCurrentTenantId_WhenTenantClaimDoesNotExist_ReturnsNull()
    {
        // Arrange
        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();
        Mock<ClaimsPrincipal> mockUser = new();

        mockUser.Setup(u => u.FindFirst("tenant-id")).Returns((Claim?)null);
        mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        ClaimsTenantStore store = new(mockHttpContextAccessor.Object);

        // Act
        string? result = store.GetCurrentTenantId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentTenantId_WhenUserIsNull_ReturnsNull()
    {
        // Arrange
        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();

        mockHttpContext.Setup(c => c.User).Returns((ClaimsPrincipal?)null);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        ClaimsTenantStore store = new(mockHttpContextAccessor.Object);

        // Act
        string? result = store.GetCurrentTenantId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentTenantId_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        ClaimsTenantStore store = new(mockHttpContextAccessor.Object);

        // Act
        string? result = store.GetCurrentTenantId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentTenantId_WithEmptyTenantClaim_ReturnsEmptyString()
    {
        // Arrange
        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();
        Mock<ClaimsPrincipal> mockUser = new();

        Claim tenantClaim = new("tenant-id", string.Empty);
        mockUser.Setup(u => u.FindFirst("tenant-id")).Returns(tenantClaim);
        mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        ClaimsTenantStore store = new(mockHttpContextAccessor.Object);

        // Act
        string? result = store.GetCurrentTenantId();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetCurrentTenantId_WithMultipleClaims_ReturnsFirstTenantClaim()
    {
        // Arrange
        const string expectedTenantId = "tenant1";
        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
        Mock<HttpContext> mockHttpContext = new();

        Claim[] claims = new[]
        {
        new Claim("user-id", "user123"),
        new Claim("tenant-id", expectedTenantId),
        new Claim("role", "admin")
    };

        ClaimsIdentity identity = new(claims);
        ClaimsPrincipal user = new(identity);

        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        ClaimsTenantStore store = new(mockHttpContextAccessor.Object);

        // Act
        string? result = store.GetCurrentTenantId();

        // Assert
        Assert.Equal(expectedTenantId, result);
    }
}
