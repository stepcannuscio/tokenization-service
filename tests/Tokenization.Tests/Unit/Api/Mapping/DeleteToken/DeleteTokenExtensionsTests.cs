using FluentAssertions;
using Tokenization.Api.Mapping.DeleteToken;
using Tokenization.Api.Requests.v1;
using Xunit;

namespace Tokenization.Tests.Unit.Api.Mapping.DeleteToken;

/// <summary>
/// Unit tests for the DeleteTokenExtensions to ensure proper extension method functionality.
/// </summary>
public class DeleteTokenExtensionsTests
{
    [Fact]
    public void ToDeleteTokenCommand_WithValidRequest_ShouldMapCorrectly()
    {
        // Arrange
        var request = new DeleteTokenRequest
        {
            Token = "tok_987654321"
        };

        // Act
        var command = request.ToDeleteTokenCommand();

        // Assert
        command.Should().NotBeNull();
        command.Token.Should().Be("tok_987654321");
    }

    [Fact]
    public void ToDeleteTokenCommand_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        DeleteTokenRequest request = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => request.ToDeleteTokenCommand());
        exception.ParamName.Should().Be("request");
    }

    [Theory]
    [InlineData("tok_abc123")]
    [InlineData("tok_xyz789")]
    [InlineData("token_with_underscores")]
    [InlineData("token-with-dashes")]
    public void ToDeleteTokenCommand_WithDifferentTokenFormats_ShouldMapCorrectly(string token)
    {
        // Arrange
        var request = new DeleteTokenRequest
        {
            Token = token
        };

        // Act
        var command = request.ToDeleteTokenCommand();

        // Assert
        command.Should().NotBeNull();
        command.Token.Should().Be(token);
    }
}