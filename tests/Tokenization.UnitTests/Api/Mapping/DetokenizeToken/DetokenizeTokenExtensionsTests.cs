using FluentAssertions;
using Tokenization.Api.Mapping.DetokenizeToken;
using Tokenization.Api.Requests.v1;
using Tokenization.Domain.ValueObjects;
using Tokenization.Tests.Shared.Utils.ValueObjects;
using Xunit;

namespace Tokenization.Tests.Unit.Api.Mapping.DetokenizeToken;

/// <summary>
/// Unit tests for the DetokenizeTokenExtensions to ensure proper extension method functionality.
/// </summary>
public class DetokenizeTokenExtensionsTests
{
    [Fact]
    public void ToDetokenizeTokenCommand_WithValidRequest_ShouldMapCorrectly()
    {
        // Arrange
        var request = new DetokenizeTokenRequest
        {
            Token = "tok_987654321"
        };

        // Act
        var command = request.ToDetokenizeTokenCommand();

        // Assert
        command.Should().NotBeNull();
        command.Token.Should().Be("tok_987654321");
    }

    [Fact]
    public void ToDetokenizeTokenCommand_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        DetokenizeTokenRequest request = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => request.ToDetokenizeTokenCommand());
        exception.ParamName.Should().Be("request");
    }

    [Fact]
    public void ToDetokenizeTokenResponse_WithValidDetokenizedToken_ShouldMapCorrectly()
    {
        // Arrange
        var tokenSummary = TestTokenSummary.Valid();

        var detokenizedToken = new DetokenizedToken(
            Plaintext: "card|4111111111111111|12|2030|John Doe",
            TokenSummary: tokenSummary
        );

        // Act
        var response = detokenizedToken.ToDetokenizeTokenResponse();

        // Assert
        response.Should().NotBeNull();
        response.Pan.Should().Be("4111111111111111");
        response.ExpMonth.Should().Be(12);
        response.ExpYear.Should().Be(2030);
    }

    [Fact]
    public void ToDetokenizeTokenResponse_WithNullDetokenizedToken_ShouldThrowArgumentNullException()
    {
        // Arrange
        DetokenizedToken detokenizedToken = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => detokenizedToken.ToDetokenizeTokenResponse());
        exception.Should().NotBeNull();
    }
}
