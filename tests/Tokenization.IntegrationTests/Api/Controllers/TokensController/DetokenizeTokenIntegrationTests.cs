using System.Net;
using System.Text.Json;
using FluentAssertions;
using Tokenization.Api.Idempotency;
using Tokenization.Api.Responses;
using Tokenization.Tests.Shared.Fixtures;
using Tokenization.Tests.Shared.Utils.Requests;
using Xunit;

namespace Tokenization.Tests.Integration.Api.Controllers.TokensController;

public class DetokenizeTokenIntegrationTests(WebApplicationFactoryFixture factory)
    : IClassFixture<WebApplicationFactoryFixture>
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task DetokenizeToken_WithValidToken_ShouldReturn200OK()
    {
        // Arrange - Create a token first
        var client = factory.CreateClient();
        var createRequest = TestCreateTokenRequest.Valid();
        client.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, Guid.NewGuid().ToString());

        var createResponse = await client.PostAsJsonAsync("/api/v1/tokens", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "token must be created successfully before detokenizing");
        var createdToken = JsonSerializer.Deserialize<CreateTokenResponse>(
            await createResponse.Content.ReadAsStringAsync(), _jsonOptions);

        // Act - Detokenize the token
        var response = await client.PostAsync($"/api/v1/tokens/{createdToken!.Token}/detokenize", null);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);
        var detokenizeResponse = JsonSerializer.Deserialize<DetokenizeTokenResponse>(content, _jsonOptions);
        detokenizeResponse.Should().NotBeNull();
        detokenizeResponse.Pan.Should().Be(createRequest.Pan);
        detokenizeResponse.ExpMonth.Should().Be(createRequest.ExpirationMonth);
        detokenizeResponse.ExpYear.Should().Be(createRequest.ExpirationYear);
        detokenizeResponse.CardholderName.Should().Be(createRequest.CardholderName);
        detokenizeResponse.PaymentMethodType.Should().Be(createRequest.PaymentMethodType);
        detokenizeResponse.Network.Should().Be(createRequest.Network);
    }

    [Fact]
    public async Task DetokenizeToken_WithNonExistentToken_ShouldReturn404NotFound()
    {
        // Arrange
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(IdempotencyHeaders.IdempotencyKey, Guid.NewGuid().ToString());

        const string nonExistentToken = "tok_nonexistent789";

        // Act
        var response = await client.PostAsync($"/api/v1/tokens/{nonExistentToken}/detokenize", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
