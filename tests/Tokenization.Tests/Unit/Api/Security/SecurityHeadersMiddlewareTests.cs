using FluentAssertions;
using Moq;
using Tokenization.Api.Security;
using Xunit;

namespace Tokenization.Tests.Unit.Api.Security;

/// <summary>
/// Unit tests for SecurityHeadersMiddleware to ensure proper security headers are applied.
/// </summary>
public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldAddContentSecurityPolicyHeader()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var next = new Mock<RequestDelegate>();
        var middleware = new SecurityHeadersMiddleware(next.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.Headers.Should().ContainKey("Content-Security-Policy");
        var cspHeader = httpContext.Response.Headers.ContentSecurityPolicy.ToString();
        cspHeader.Should().Contain("default-src 'self'");
        cspHeader.Should().Contain("script-src 'self' 'unsafe-inline' 'unsafe-eval'");
        cspHeader.Should().Contain("style-src 'self' 'unsafe-inline'");
        cspHeader.Should().Contain("img-src 'self' data: https:");
        cspHeader.Should().Contain("font-src 'self' data:");
        cspHeader.Should().Contain("connect-src 'self'");
        cspHeader.Should().Contain("frame-ancestors 'none'");
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldAddXContentTypeOptionsHeader()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var next = new Mock<RequestDelegate>();
        var middleware = new SecurityHeadersMiddleware(next.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.Headers.Should().ContainKey("X-Content-Type-Options");
        var header = httpContext.Response.Headers["X-Content-Type-Options"].ToString();
        header.Should().Be("nosniff");
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldAddXXSSProtectionHeader()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var next = new Mock<RequestDelegate>();
        var middleware = new SecurityHeadersMiddleware(next.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.Headers.Should().ContainKey("X-XSS-Protection");
        var header = httpContext.Response.Headers["X-XSS-Protection"].ToString();
        header.Should().Be("1; mode=block");
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldAddXFrameOptionsHeader()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var next = new Mock<RequestDelegate>();
        var middleware = new SecurityHeadersMiddleware(next.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.Headers.Should().ContainKey("X-Frame-Options");
        var header = httpContext.Response.Headers["X-Frame-Options"].ToString();
        header.Should().Be("DENY");
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldAddReferrerPolicyHeader()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var next = new Mock<RequestDelegate>();
        var middleware = new SecurityHeadersMiddleware(next.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.Headers.Should().ContainKey("Referrer-Policy");
        var header = httpContext.Response.Headers["Referrer-Policy"].ToString();
        header.Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldAddPermissionsPolicyHeader()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var next = new Mock<RequestDelegate>();
        var middleware = new SecurityHeadersMiddleware(next.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.Headers.Should().ContainKey("Permissions-Policy");
        var header = httpContext.Response.Headers["Permissions-Policy"].ToString();
        header.Should().Contain("camera=()");
        header.Should().Contain("microphone=()");
        header.Should().Contain("geolocation=()");
        header.Should().Contain("interest-cohort=()");
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldAddPCIComplianceHeaders()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var next = new Mock<RequestDelegate>();
        var middleware = new SecurityHeadersMiddleware(next.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.Headers.Should().ContainKey("X-PCI-Compliant");
        var pciHeader = httpContext.Response.Headers["X-PCI-Compliant"].ToString();
        pciHeader.Should().Be("true");

        httpContext.Response.Headers.Should().ContainKey("X-Security-Level");
        var securityLevelHeader = httpContext.Response.Headers["X-Security-Level"].ToString();
        securityLevelHeader.Should().Be("PCI-DSS");
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldAddCacheControlHeaders()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var next = new Mock<RequestDelegate>();
        var middleware = new SecurityHeadersMiddleware(next.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.Headers.Should().ContainKey("Cache-Control");
        var cacheControlHeader = httpContext.Response.Headers["Cache-Control"].ToString();
        cacheControlHeader.Should().Contain("no-store");
        cacheControlHeader.Should().Contain("no-cache");
        cacheControlHeader.Should().Contain("must-revalidate");
        cacheControlHeader.Should().Contain("private");

        httpContext.Response.Headers.Should().ContainKey("Pragma");
        var pragmaHeader = httpContext.Response.Headers["Pragma"].ToString();
        pragmaHeader.Should().Be("no-cache");

        httpContext.Response.Headers.Should().ContainKey("Expires");
        var expiresHeader = httpContext.Response.Headers["Expires"].ToString();
        expiresHeader.Should().Be("0");
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldCallNextMiddleware()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var next = new Mock<RequestDelegate>();
        var middleware = new SecurityHeadersMiddleware(next.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        next.Verify(x => x(httpContext), Times.Once);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream()
            }
        };
        return context;
    }
}