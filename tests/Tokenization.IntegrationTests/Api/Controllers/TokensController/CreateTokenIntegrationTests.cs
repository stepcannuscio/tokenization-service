using System.Net;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokenization.Api.Authorization;
using Tokenization.Api.Idempotency;
using Tokenization.Api.Requests.v1;
using Tokenization.Api.Responses;
using Tokenization.Tests.Shared.Fixtures;
using Tokenization.Tests.Shared.Utils.Authentication;
using Tokenization.Tests.Shared.Utils.Mediatr;
using Tokenization.Tests.Shared.Utils.Requests;
using Xunit;

namespace Tokenization.Tests.Integration.Api.Controllers.TokensController;

/// <summary>
/// Comprehensive integration tests for the CreateToken endpoint.
/// Tests authentication, authorization, idempotency, multi-tenant isolation, validation,
/// versioning, exception handling, and security headers.
/// </summary>
public class CreateTokenIntegrationTests(WebApplicationFactoryFixture factory)
    : IClassFixture<WebApplicationFactoryFixture>
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task CreateToken_WithValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = TestCreateTokenRequest.Valid();
        var idempotencyKey = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, idempotencyKey);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/tokens", request);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v");

        var tokenResponse = JsonSerializer.Deserialize<CreateTokenResponse>(responseContent, _jsonOptions);
        tokenResponse.Should().NotBeNull();
        tokenResponse.Token.Should().NotBeNullOrEmpty();
        tokenResponse.MaskedData.Should().NotBeNullOrEmpty();
        tokenResponse.Last4.Should().Be(request.Pan[^4..]);
        tokenResponse.PaymentMethodType.Should().Be(request.PaymentMethodType);
        tokenResponse.Network.Should().Be(request.Network);
    }

    [Fact]
    public async Task CreateToken_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange - Create client with authentication that fails
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTestAuthentication("Test-Fail", options =>
                {
                    options.DefaultUserId = null!; // This will cause authentication to fail
                });
            });
        }).CreateClient();

        var request = TestCreateTokenRequest.Valid();
        client.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, Guid.NewGuid().ToString());

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/tokens", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateToken_WithInvalidPan_ShouldReturn422UnprocessableEntity()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = new CreateTokenRequest
        {
            Pan = "invalid", // Invalid PAN
            ExpirationMonth = 12,
            ExpirationYear = 2025,
            CardholderName = "John Doe",
            Network = "Visa",
            CustomerId = "customer-456",
            PaymentMethodType = "CreditCard",
            TokenType = "OneTime"
        };
        client.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, Guid.NewGuid().ToString());

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/tokens", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateToken_WithMissingIdempotencyKey_ShouldReturn400BadRequest()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = TestCreateTokenRequest.Valid();
        // Deliberately not adding idempotency key

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/tokens", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateToken_WithIdempotentRequests_ShouldReturnSameResponse()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = TestCreateTokenRequest.Valid();
        var idempotencyKey = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, idempotencyKey);

        // Act - First request
        var response1 = await client.PostAsJsonAsync("/api/v1/tokens", request);
        var content1 = await response1.Content.ReadAsStringAsync();

        // Act - Second request with same idempotency key
        var response2 = await client.PostAsJsonAsync("/api/v1/tokens", request);
        var content2 = await response2.Content.ReadAsStringAsync();

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
        content1.Should().Be(content2); // Responses should be identical
        response2.Headers.Should().ContainKey(IdempotencyHeaders.IdempotencyReplay);
        response2.Headers.GetValues(IdempotencyHeaders.IdempotencyReplay).Should().Contain("true");
    }

    [Fact]
    public async Task CreateToken_WithDifferentTenants_ShouldCreateSeparateTokens()
    {
        // Arrange - Client 1 with tenant A
        var client1 = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTestAuthentication("Test-Tenant-A", options =>
                {
                    options.DefaultUserId = Guid.NewGuid().ToString();
                    options.DefaultTenantId = "tenant-A";
                });
            });
        }).CreateClient();

        client1.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, Guid.NewGuid().ToString());

        var request1 = TestCreateTokenRequest.Valid();
        var response1 = await client1.PostAsJsonAsync("/api/v1/tokens", request1);

        // Arrange - Client 2 with tenant B
        var client2 = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTestAuthentication("Test-Tenant-B", options =>
                {
                    options.DefaultUserId = Guid.NewGuid().ToString();
                    options.DefaultTenantId = "tenant-B";
                });
            });
        }).CreateClient();

        client2.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, Guid.NewGuid().ToString());

        var request2 = TestCreateTokenRequest.Valid();
        var response2 = await client2.PostAsJsonAsync("/api/v1/tokens", request2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        var token1 = JsonSerializer.Deserialize<CreateTokenResponse>(
            await response1.Content.ReadAsStringAsync(), _jsonOptions);
        var token2 = JsonSerializer.Deserialize<CreateTokenResponse>(
            await response2.Content.ReadAsStringAsync(), _jsonOptions);

        token1!.Token.Should().NotBe(token2!.Token); // Different tokens for different tenants
    }

    [Fact]
    public async Task CreateToken_WithVersionInUrl_ShouldWork()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = TestCreateTokenRequest.Valid();
        client.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, Guid.NewGuid().ToString());

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/tokens", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateToken_WithVersionInHeader_ShouldWork()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = TestCreateTokenRequest.Valid();
        client.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-API-Version", "1.0");

        // Act - Use versioned URL
        var response = await client.PostAsJsonAsync("/api/v1/tokens", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateToken_WithInsufficientPermissions_ShouldReturn403Forbidden()
    {
        // Arrange - Create client with authentication but insufficient scopes
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTestAuthentication("Test-Insufficient", options =>
                {
                    options.DefaultUserId = Guid.NewGuid().ToString();
                    options.DefaultTenantId = "tenant-123";
                    options.DefaultScopes = [Scopes.TokenRead]; // Only read scope, not create
                    options.DefaultRoles = []; // No admin role
                });
            });
        }).CreateClient();

        var request = TestCreateTokenRequest.Valid();
        client.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, Guid.NewGuid().ToString());

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/tokens", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateToken_WithExceptionHandling_ShouldReturnProperErrorResponse()
    {
        // Arrange - Create client that will trigger an exception in the application
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Override the mediator to throw an exception
                services.AddScoped<IMediator>(_ => new InvalidMediator());
            });
        }).CreateClient();

        var request = TestCreateTokenRequest.Valid();
        client.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, Guid.NewGuid().ToString());

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/tokens", request);
        var responseContent = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        problemDetails.Should().NotBeNull();
        problemDetails.Status.Should().Be(422);
    }

    [Fact]
    public async Task CreateToken_WithSecurityHeaders_ShouldIncludeAllSecurityHeaders()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = TestCreateTokenRequest.Valid();
        client.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, Guid.NewGuid().ToString());

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/tokens", request);

        // Assert
        response.Headers.Should().ContainKey("Content-Security-Policy");
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-XSS-Protection");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.Should().ContainKey("Permissions-Policy");
        response.Headers.Should().ContainKey("X-PCI-Compliant");
        response.Headers.Should().ContainKey("X-Security-Level");
        response.Headers.Should().ContainKey("Cache-Control");
        response.Headers.Should().ContainKey("Pragma");
    }
}
