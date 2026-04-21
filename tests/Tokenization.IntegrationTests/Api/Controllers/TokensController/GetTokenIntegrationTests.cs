using FluentAssertions;
using System.Net;
using System.Text.Json;
using Tokenization.Api.Idempotency;
using Tokenization.Api.Responses;
using Tokenization.Tests.Shared.Fixtures;
using Tokenization.Tests.Shared.Utils.Requests;
using Xunit;

namespace Tokenization.Tests.Integration.Api.Controllers.TokensController;

public class GetTokenIntegrationTests(WebApplicationFactoryFixture factory)
    : IClassFixture<WebApplicationFactoryFixture>
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task GetToken_WithValidToken_ShouldReturn200OK()
    {
        // Arrange - Create a token first
        var client = factory.CreateClient();
        var createRequest = TestCreateTokenRequest.Valid();
        client.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, Guid.NewGuid().ToString());
        
        var createResponse = await client.PostAsJsonAsync("/api/v1/tokens", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdToken = JsonSerializer.Deserialize<CreateTokenResponse>(createContent, _jsonOptions);

        // Act - Get the token
        var response = await client.GetAsync($"/api/v1/tokens/{createdToken!.Token}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenResponse = JsonSerializer.Deserialize<GetTokenResponse>(content, _jsonOptions);
        tokenResponse.Should().NotBeNull();
        tokenResponse.Token.Should().Be(createdToken.Token);
        tokenResponse.MaskedData.Should().NotBeNullOrEmpty();
        tokenResponse.Last4.Should().Be(createRequest.Pan[^4..]);
        tokenResponse.CustomerId.Should().Be(createRequest.CustomerId);
    }

    [Fact]
    public async Task GetToken_WithNonExistentToken_ShouldReturn404NotFound()
    {
        // Arrange
        var client = factory.CreateClient();
        const string nonExistentToken = "tok_nonexistent123";

        // Act
        var response = await client.GetAsync($"/api/v1/tokens/{nonExistentToken}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
