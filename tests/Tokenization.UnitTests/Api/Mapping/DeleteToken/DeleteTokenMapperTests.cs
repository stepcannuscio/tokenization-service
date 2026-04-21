using FluentAssertions;
using Tokenization.Api.Mapping.DeleteToken;
using Tokenization.Api.Requests.v1;
using Xunit;

namespace Tokenization.Tests.Unit.Api.Mapping.DeleteToken;

/// <summary>
/// Unit tests for the DeleteTokenMapper to ensure proper mapping between API and application layers.
/// </summary>
public class DeleteTokenMapperTests
{
    [Fact]
    public void MapRequest_WithValidRequest_ShouldMapCorrectly()
    {
        // Arrange
        var request = new DeleteTokenRequest
        {
            Token = "tok_123456789"
        };
        var mapper = new DeleteTokenMapper();

        // Act
        var command = mapper.MapRequest(request);

        // Assert
        command.Should().NotBeNull();
        command.Token.Should().Be("tok_123456789");
    }

    [Fact]
    public void MapRequest_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mapper = new DeleteTokenMapper();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => mapper.MapRequest(null!));
        exception.ParamName.Should().Be("request");
    }

    [Fact]
    public void MapResponse_WithTrueResult_ShouldReturnTrue()
    {
        // Arrange
        var mapper = new DeleteTokenMapper();
        var result = true;

        // Act
        var response = mapper.MapResponse(result);

        // Assert
        response.Should().BeTrue();
    }

    [Fact]
    public void MapResponse_WithFalseResult_ShouldReturnFalse()
    {
        // Arrange
        var mapper = new DeleteTokenMapper();
        var result = false;

        // Act
        var response = mapper.MapResponse(result);

        // Assert
        response.Should().BeFalse();
    }
}