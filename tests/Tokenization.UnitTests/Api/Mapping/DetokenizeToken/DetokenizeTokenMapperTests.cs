using FluentAssertions;
using Tokenization.Api.Mapping.DetokenizeToken;
using Tokenization.Api.Requests.v1;
using Tokenization.Domain.Enums;
using Tokenization.Domain.ValueObjects;
using Xunit;

namespace Tokenization.Tests.Unit.Api.Mapping.DetokenizeToken;

/// <summary>
/// Unit tests for the DetokenizeTokenMapper to ensure proper mapping between API and application layers.
/// </summary>
public class DetokenizeTokenMapperTests
{
    [Fact]
    public void MapRequest_WithValidRequest_ShouldMapCorrectly()
    {
        // Arrange
        var request = new DetokenizeTokenRequest
        {
            Token = "tok_123456789"
        };
        var mapper = new DetokenizeTokenMapper();

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
        var mapper = new DetokenizeTokenMapper();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => mapper.MapRequest(null!));
        exception.ParamName.Should().Be("request");
    }

    [Fact]
    public void MapResponse_WithValidDetokenizedToken_ShouldMapCorrectly()
    {
        // Arrange
        var mapper = new DetokenizeTokenMapper();
        var tokenSummary = new TokenSummary(
            Token: "tok_123456789",
            MaskedData: "****1111",
            Last4: "1111",
            PaymentMethodType: PaymentMethodType.Card,
            Network: "Visa",
            Currency: "USD",
            Country: "US",
            TenantId: "tenant_123",
            CustomerId: "customer_456",
            TokenType: TokenType.OneTime,
            UsageCount: 0,
            MaxUses: 1,
            IsActive: true,
            CreatedAt: DateTimeOffset.UtcNow,
            LastUsedAt: null,
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(30)
        );

        var detokenizedToken = new DetokenizedToken(
            Plaintext: "card|4111111111111111|12|2030|John Doe",
            TokenSummary: tokenSummary
        );

        // Act
        var response = mapper.MapResponse(detokenizedToken);

        // Assert
        response.Should().NotBeNull();
        response.Pan.Should().Be("4111111111111111");
        response.ExpMonth.Should().Be(12);
        response.ExpYear.Should().Be(2030);
        response.CardholderName.Should().Be("John Doe");
        response.PaymentMethodType.Should().Be("Card");
        response.Network.Should().Be("Visa");
    }

    [Fact]
    public void MapResponse_WithNullDetokenizedToken_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mapper = new DetokenizeTokenMapper();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => mapper.MapResponse(null!));
        exception.ParamName.Should().Be("detokenizedToken");
    }

    [Theory]
    [InlineData("card|4111111111111111|1|2025|Alice Smith", "4111111111111111", 1, 2025, "Alice Smith")]
    [InlineData("card|5555555555554444|6|2026|Bob Johnson", "5555555555554444", 6, 2026, "Bob Johnson")]
    [InlineData("card|378282246310005|3|2027|", "378282246310005", 3, 2027, null)]
    [InlineData("card|6011111111111117|9|2028|Charlie Brown", "6011111111111117", 9, 2028, "Charlie Brown")]
    public void MapResponse_WithDifferentCardData_ShouldMapCorrectly(
        string plaintext, string expectedPan, int expectedMonth, int expectedYear, string? expectedName)
    {
        // Arrange
        var mapper = new DetokenizeTokenMapper();
        var tokenSummary = new TokenSummary(
            Token: "tok_test",
            MaskedData: "****1111",
            Last4: "1111",
            PaymentMethodType: PaymentMethodType.Card,
            Network: "Visa",
            Currency: "USD",
            Country: "US",
            TenantId: "tenant_123",
            CustomerId: "customer_456",
            TokenType: TokenType.OneTime,
            UsageCount: 0,
            MaxUses: 1,
            IsActive: true,
            CreatedAt: DateTimeOffset.UtcNow,
            LastUsedAt: null,
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(30)
        );

        var detokenizedToken = new DetokenizedToken(plaintext, tokenSummary);

        // Act
        var response = mapper.MapResponse(detokenizedToken);

        // Assert
        response.Should().NotBeNull();
        response.Pan.Should().Be(expectedPan);
        response.ExpMonth.Should().Be(expectedMonth);
        response.ExpYear.Should().Be(expectedYear);
        response.CardholderName.Should().Be(expectedName);
    }
}
