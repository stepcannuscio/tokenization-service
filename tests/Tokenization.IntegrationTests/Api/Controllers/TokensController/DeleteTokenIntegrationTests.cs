using FluentAssertions;
using System.Net;
using System.Text.Json;
using Tokenization.Api.Idempotency;
using Tokenization.Api.Responses;
using Tokenization.Tests.Shared.Fixtures;
using Tokenization.Tests.Shared.Utils.Requests;
using Xunit;

namespace Tokenization.Tests.Integration.Api.Controllers.TokensController;

[Collection("IntegrationTests")]
public class DeleteTokenIntegrationTests(WebApplicationFactoryFixture factory)
    : IClassFixture<WebApplicationFactoryFixture>
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task DeleteToken_WithValidToken_ShouldReturn204NoContent()
    {
        // Arrange - Create a token first
        var client = factory.CreateClient();
        var createRequest = TestCreateTokenRequest.Valid();
        client.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, Guid.NewGuid().ToString());
        
        var createResponse = await client.PostAsJsonAsync("/api/v1/tokens", createRequest);
        var createdToken = JsonSerializer.Deserialize<CreateTokenResponse>(
            await createResponse.Content.ReadAsStringAsync(), _jsonOptions);

        // Act - Delete the token
        var response = await client.DeleteAsync($"/api/v1/tokens/{createdToken!.Token}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify token is deleted by trying to get it
        var getResponse = await client.GetAsync($"/api/v1/tokens/{createdToken.Token}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteToken_WithNonExistentToken_ShouldReturn404NotFound()
    {
        // Arrange
        var client = factory.CreateClient();
        const string nonExistentToken = "tok_nonexistent456";

        // Act
        var response = await client.DeleteAsync($"/api/v1/tokens/{nonExistentToken}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}