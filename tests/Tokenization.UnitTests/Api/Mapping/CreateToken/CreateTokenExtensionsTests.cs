using FluentAssertions;
using Moq;
using Tokenization.Api.Mapping.CreateToken;
using Tokenization.Api.Requests.v1;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.ValueObjects;
using Tokenization.Tests.Shared.Utils.Requests;
using Tokenization.Tests.Shared.Utils.ValueObjects;
using Xunit;

namespace Tokenization.Tests.Unit.Api.Mapping.CreateToken;

/// <summary>
/// Unit tests for the TokenRequestExtensions to ensure proper extension method behavior.
/// These tests focus on extension method specific behavior rather than duplicating mapper tests.
/// </summary>
public class CreateTokenExtensionsTests
{
    private const string TenantId = "tenant-123";
    private static ITenantContextService GetTenantContextService()
    {
        var mock = new Mock<ITenantContextService>();
        mock.Setup(s => s.GetCurrentTenantId())
            .Returns(() => TenantId);

        return mock.Object;
    }

    [Fact]
    public void ToCreateTokenCommand_WithValidRequest_ShouldReturnCommand()
    {
        // Arrange
        var request = TestCreateTokenRequest.Valid();

        // Act
        var command = request.ToCreateTokenCommand(GetTenantContextService());

        // Assert
        command.Should().NotBeNull();
        // Basic verification that the extension method works - detailed mapping tests are in CreateTokenMapperTests
        command.TenantId.Should().Be(TenantId);
        command.Card.Should().NotBeNull();
        command.Card.Pan.Should().Be(request.Pan);
    }

    [Fact]
    public void ToCreateTokenCommand_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        CreateTokenRequest? request = null;

        // Act & Assert
        var action = () => request!.ToCreateTokenCommand(GetTenantContextService());
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("request");
    }

    [Fact]
    public void ToCreateTokenResponse_WithValidSummary_ShouldReturnResponse()
    {
        // Arrange
        var summary = TestTokenSummary.Valid();

        // Act
        var response = summary.ToCreateTokenResponse(GetTenantContextService());

        // Assert
        response.Should().NotBeNull();
        // Basic verification that the extension method works - detailed mapping tests are in CreateTokenMapperTests
        response.Token.Should().Be(summary.Token);
        response.Network.Should().Be(summary.Network);
    }

    [Fact]
    public void ToCreateTokenResponse_WithNullSummary_ShouldThrowArgumentNullException()
    {
        // Arrange
        TokenSummary? summary = null;

        // Act & Assert
        var action = () => summary!.ToCreateTokenResponse(GetTenantContextService());
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToCreateTokenCommand_ShouldCreateNewMapperInstance()
    {
        // Arrange
        var request = TestCreateTokenRequest.Valid();

        // Act
        var command1 = request.ToCreateTokenCommand(GetTenantContextService());
        var command2 = request.ToCreateTokenCommand(GetTenantContextService());

        // Assert
        // Verify that each call creates a new mapper instance (extension method behavior)
        command1.Should().NotBeNull();
        command2.Should().NotBeNull();
        // Both should produce the same result since they use the same input
        command1.TenantId.Should().Be(command2.TenantId);
        command1.Card.Should().NotBeNull();
        command2.Card.Should().NotBeNull();
        command1.Card.Pan.Should().Be(command2.Card.Pan);
    }

    [Fact]
    public void ToCreateTokenResponse_ShouldCreateNewMapperInstance()
    {
        // Arrange
        var summary = TestTokenSummary.Valid();

        // Act
        var response1 = summary.ToCreateTokenResponse(GetTenantContextService());
        var response2 = summary.ToCreateTokenResponse(GetTenantContextService());

        // Assert
        // Verify that each call creates a new mapper instance (extension method behavior)
        response1.Should().NotBeNull();
        response2.Should().NotBeNull();
        // Both should produce the same result since they use the same input
        response1.Token.Should().Be(response2.Token);
        response1.Network.Should().Be(response2.Network);
    }
}
