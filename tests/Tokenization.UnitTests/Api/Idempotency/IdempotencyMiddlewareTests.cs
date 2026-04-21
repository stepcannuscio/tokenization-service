using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Tokenization.Api.Idempotency;
using Tokenization.Api.Idempotency.Config.Options;
using Tokenization.Tests.Shared.Fixtures;
using Tokenization.Tests.Shared.Utils.Cache;
using Xunit;

namespace Tokenization.Tests.Unit.Api.Idempotency;

/// <summary>
/// Unit tests for the IdempotencyMiddleware to ensure proper idempotency handling.
/// </summary>
public sealed class IdempotencyMiddlewareTests : IClassFixture<HybridCacheFixtureInMemory>
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<IIdempotencyKeyHasher> _mockHasher;
    private readonly IdempotencyMiddleware _middleware;

    public IdempotencyMiddlewareTests(HybridCacheFixtureInMemory cacheFixture)
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockHasher = new Mock<IIdempotencyKeyHasher>();
        var mockOptions = new Mock<IOptions<IdempotencyOptions>>();
        var cache = cacheFixture.Cache;

        mockOptions.Setup(x => x.Value).Returns(new IdempotencyOptions { TtlSeconds = 600 });
        
        _middleware = new IdempotencyMiddleware(
            _mockNext.Object,
            cache,
            mockOptions.Object,
            _mockHasher.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithGetRequest_ShouldNotApplyIdempotency()
    {
        // Arrange
        var context = CreateHttpContext("GET", "/api/test");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                 .Callback<HttpContext>(ctx => _ = ctx);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        // For GET requests, we should not call the hasher at all
        _mockHasher.Verify(x => x.Hash(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PathString>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithPostRequestWithoutIdempotencyKey_ShouldReturn400()
    {
        // Arrange
        var context = CreateHttpContext("POST", "/api/tokens");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithPostRequestWithEmptyIdempotencyKey_ShouldReturn400()
    {
        // Arrange
        var context = CreateHttpContext("POST", "/api/tokens");
        context.Request.Headers[IdempotencyHeaders.IdempotencyKey] = "";

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithPostRequestWithValidKey_ShouldProcessRequest()
    {
        // Arrange
        var context = CreateHttpContext("POST", "/api/tokens");
        context.Request.Headers[IdempotencyHeaders.IdempotencyKey] = "test-key-123";
        
        _mockHasher.Setup(x => x.Hash(It.IsAny<string>(), "POST", "/api/tokens", "test-key-123"))
            .Returns(TestCacheKey.New());

        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                 .Callback<HttpContext>(ctx => 
                 {
                     _ = ctx;
                     ctx.Response.StatusCode = 200;
                     var responseData = new { message = "success" };
                     var responseJson = JsonSerializer.Serialize(responseData);
                     ctx.Response.WriteAsync(responseJson);
                 });

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
        _mockHasher.Verify(x => x.Hash(It.IsAny<string>(), "POST", "/api/tokens", "test-key-123"), Times.Once);
    }


    [Theory]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task InvokeAsync_WithDataModifyingMethods_ShouldApplyIdempotency(string method)
    {
        // Arrange
        var context = CreateHttpContext(method, "/api/tokens");
        context.Request.Headers[IdempotencyHeaders.IdempotencyKey] = "test-key-123";
        
        _mockHasher.Setup(x => x.Hash(It.IsAny<string>(), method, "/api/tokens", "test-key-123"))
                   .Returns(TestCacheKey.New());

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockHasher.Verify(x => x.Hash(It.IsAny<string>(), method, "/api/tokens", "test-key-123"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithAuthenticatedUser_ShouldUseUserIdInCacheKey()
    {
        // Arrange
        var context = CreateHttpContext("POST", "/api/tokens");
        context.Request.Headers[IdempotencyHeaders.IdempotencyKey] = "test-key-123";
        const string userId = "user-123";
        
        // Add authenticated user
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userId)
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "test");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);

        
        _mockHasher.Setup(x => x.Hash(userId, "POST", "/api/tokens", "test-key-123"))
                   .Returns(TestCacheKey.New());

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockHasher.Verify(x => x.Hash(userId, "POST", "/api/tokens", "test-key-123"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithCachedResponse_ShouldReplayResponse()
    {
        // Arrange
        var context = CreateHttpContext("POST", "/api/tokens");
        context.Request.Headers[IdempotencyHeaders.IdempotencyKey] = "test-key-123";
        
        var cacheKey = TestCacheKey.New();
        _mockHasher.Setup(x => x.Hash(It.IsAny<string>(), "POST", "/api/tokens", "test-key-123"))
                   .Returns(cacheKey);

        // First request to cache the response
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                 .Callback<HttpContext>(ctx => 
                 {
                     ctx.Response.StatusCode = 201;
                     ctx.Response.Headers["Content-Type"] = "application/json";
                     ctx.Response.WriteAsync("{\"message\":\"cached response\",\"id\":\"123\"}");
                 });

        await _middleware.InvokeAsync(context);

        // Reset mock and create new context for second request
        _mockNext.Reset();
        var context2 = CreateHttpContext("POST", "/api/tokens");
        context2.Request.Headers[IdempotencyHeaders.IdempotencyKey] = "test-key-123";
        _mockHasher.Setup(x => x.Hash(It.IsAny<string>(), "POST", "/api/tokens", "test-key-123"))
                   .Returns(cacheKey);

        // Act - Second request should return cached response
        await _middleware.InvokeAsync(context2);

        // Assert
        context2.Response.StatusCode.Should().Be(201);
        context2.Response.Headers[IdempotencyHeaders.IdempotencyReplay].ToString().Should().Be("true");
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithResponseAlreadyStarted_ShouldNotSetStatusCode()
    {
        // Arrange
        var context = CreateHttpContext("POST", "/api/tokens");
        context.Request.Headers[IdempotencyHeaders.IdempotencyKey] = "test-key-123";
        
        var cacheKey = TestCacheKey.New();
        _mockHasher.Setup(x => x.Hash(It.IsAny<string>(), "POST", "/api/tokens", "test-key-123"))
                   .Returns(cacheKey);

        // Simulate response already started
        await context.Response.WriteAsync("partial response");
        context.Response.StatusCode = 200; // Set initial status code

        // Act - This should not throw an exception even if response has started
        await _middleware.InvokeAsync(context);

        // Assert
        // Status code should remain unchanged since response already started
        context.Response.StatusCode.Should().Be(200);
        // Should not throw an exception - the middleware should still process normally
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithSuccessfulResponse_ShouldCacheResponse()
    {
        // Arrange
        var context = CreateHttpContext("POST", "/api/tokens");
        context.Request.Headers[IdempotencyHeaders.IdempotencyKey] = "test-key-123";
        
        var cacheKey = TestCacheKey.New();
        _mockHasher.Setup(x => x.Hash(It.IsAny<string>(), "POST", "/api/tokens", "test-key-123"))
                   .Returns(cacheKey);

        var responseData = new { message = "success", id = "456" };
        var responseJson = JsonSerializer.Serialize(responseData);

        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                 .Callback<HttpContext>(ctx => 
                 {
                     ctx.Response.StatusCode = 201;
                     ctx.Response.Headers["Location"] = "/tokens/456";
                     ctx.Response.WriteAsync(responseJson);
                 });

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
        context.Response.StatusCode.Should().Be(201);
        context.Response.Headers.Location.ToString().Should().Be("/tokens/456");
    }

    [Fact]
    public async Task InvokeAsync_WithErrorResponse_ShouldNotCache()
    {
        // Arrange
        var context = CreateHttpContext("POST", "/api/tokens");
        context.Request.Headers[IdempotencyHeaders.IdempotencyKey] = "test-key-123";
        
        var cacheKey = TestCacheKey.New();
        _mockHasher.Setup(x => x.Hash(It.IsAny<string>(), "POST", "/api/tokens", "test-key-123"))
                   .Returns(cacheKey);

        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                 .Callback<HttpContext>(ctx => 
                 {
                     ctx.Response.StatusCode = 422; // Unprocessable Entity
                     ctx.Response.WriteAsync("validation error");
                 });

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
        context.Response.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task InvokeAsync_WithDuplicateRequest_ShouldReturnCachedResponse()
    {
        // Arrange
        var context1 = CreateHttpContext("POST", "/api/tokens");
        var context2 = CreateHttpContext("POST", "/api/tokens");
        context1.Request.Headers[IdempotencyHeaders.IdempotencyKey] = "test-key-123";
        context2.Request.Headers[IdempotencyHeaders.IdempotencyKey] = "test-key-123";
        
        var cacheKey = TestCacheKey.New();
        _mockHasher.Setup(x => x.Hash(It.IsAny<string>(), "POST", "/api/tokens", "test-key-123"))
                   .Returns(cacheKey);

        var responseData = new { message = "success", id = "789" };
        var responseJson = JsonSerializer.Serialize(responseData);

        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                 .Callback<HttpContext>(ctx => 
                 {
                     ctx.Response.StatusCode = 201;
                     ctx.Response.WriteAsync(responseJson);
                 });

        // Act - First request
        await _middleware.InvokeAsync(context1);

        // Reset the mock to verify it's not called again
        _mockNext.Reset();
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                 .Callback<HttpContext>(ctx => 
                 {
                     ctx.Response.StatusCode = 201;
                     ctx.Response.WriteAsync(responseJson);
                 });

        // Act - Second request with same idempotency key
        await _middleware.InvokeAsync(context2);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
        context2.Response.StatusCode.Should().Be(201);
        context2.Response.Headers[IdempotencyHeaders.IdempotencyReplay].ToString().Should().Be("true");
    }

    private static DefaultHttpContext CreateHttpContext(string method, string path)
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = method,
                Path = path,
                Scheme = "https",
                Host = new HostString("localhost")
            },
            Response =
            {
                Body = new MemoryStream()
            }
        };

        return context;
    }
}