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
/// Unit tests for the CreateTokenMapper to ensure proper mapping between API and application layers.
/// </summary>
public class CreateTokenMapperTests
{
    private const string TenantId = "tenant-123";
    private static ITenantContextService GetTenantContextService()
    {
        var mock = new Mock<ITenantContextService>();
        mock.Setup(s => s.GetCurrentTenantId())
            .Returns(() => TenantId);

        return mock.Object;
    }

    private readonly CreateTokenMapper _mapper = new(GetTenantContextService());

    [Fact]
    public void MapRequest_WithValidRequest_ShouldMapToCommand()
    {
        // Arrange
        var request = TestCreateTokenRequest.Valid();

        // Act
        var command = _mapper.MapRequest(request);

        // Assert
        command.Should().NotBeNull();
        command.CustomerId.Should().Be(request.CustomerId);
        command.TenantId.Should().Be(TenantId);
        command.PaymentMethodType.Should().Be(request.PaymentMethodType);
        command.StoredCredentialInitiator.Should().Be(request.StoredCredentialInitiator);
        command.Network.Should().Be(request.Network);
        command.Card.Should().NotBeNull();
        command.Card.Pan.Should().Be(request.Pan);
        command.Card.ExpMonth.Should().Be(request.ExpirationMonth);
        command.Card.ExpYear.Should().Be(request.ExpirationYear);
        command.Card.CardholderName.Should().Be(request.CardholderName);
    }

    [Fact]
    public void MapRequest_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        CreateTokenRequest? request = null;

        // Act & Assert
        var action = () => _mapper.MapRequest(request!);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("request");
    }

    [Fact]
    public void MapResponse_WithValidSummary_ShouldMapToResponse()
    {
        // Arrange
        var summary = TestTokenSummary.Valid();

        // Act
        var response = _mapper.MapResponse(summary);

        // Assert
        response.Should().NotBeNull();
        response.Token.Should().Be(summary.Token);
        response.MaskedData.Should().Be(summary.MaskedData);
        response.Last4.Should().Be(summary.Last4);
        response.PaymentMethodType.Should().Be(summary.PaymentMethodType.ToString());
        response.Network.Should().Be(summary.Network);
    }

    [Fact]
    public void MapResponse_WithNullSummary_ShouldThrowArgumentNullException()
    {
        // Arrange
        TokenSummary? summary = null;

        // Act & Assert
        var action = () => _mapper.MapResponse(summary!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("4111111111111111", "Visa")]
    [InlineData("5555555555554444", "Mastercard")]
    [InlineData("378282246310005", "American Express")]
    [InlineData("6011111111111117", "Discover")]
    public void MapRequest_WithDifferentCardTypes_ShouldMapCorrectly(string pan, string network)
    {
        // Arrange
        var request = new CreateTokenRequest
        {
            Pan = pan,
            Network = network,
            ExpirationMonth = 12,
            ExpirationYear = 2025,
            CardholderName = "John Doe",
            CustomerId = "customer-456",
            PaymentMethodType = "Card",
            TokenType = "OneTime"
        };

        // Act
        var command = _mapper.MapRequest(request);

        // Assert
        command.Card.Should().NotBeNull();
        command.Card.Pan.Should().Be(pan);
        command.Network.Should().Be(network);
    }

    [Theory]
    [InlineData("Card")]
    [InlineData("Paypal")]
    [InlineData("Alipay")]
    public void MapRequest_WithDifferentPaymentMethodTypes_ShouldMapCorrectly(string paymentMethodType)
    {
        // Arrange
        var request = new CreateTokenRequest
        {
            Pan = "4111111111111111",
            Network = "Visa",
            ExpirationMonth = 12,
            ExpirationYear = 2025,
            CardholderName = "John Doe",
            CustomerId = "customer-456",
            PaymentMethodType = paymentMethodType,
            TokenType = "OneTime"
        };

        // Act
        var command = _mapper.MapRequest(request);

        // Assert
        command.PaymentMethodType.Should().Be(paymentMethodType);
    }

    [Theory]
    [InlineData("OneTime")]
    [InlineData("StoredCredential")]
    public void MapRequest_WithDifferentTokenTypes_ShouldMapCorrectly(string tokenType)
    {
        // Arrange
        var request = new CreateTokenRequest
        {
            Pan = "4111111111111111",
            Network = "Visa",
            ExpirationMonth = 12,
            ExpirationYear = 2025,
            CardholderName = "John Doe",
            CustomerId = "customer-456",
            PaymentMethodType = "Card",
            TokenType = tokenType
        };

        // Act
        var command = _mapper.MapRequest(request);

        // Assert
        command.TokenType.Should().Be(tokenType);
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Merchant")]
    public void MapRequest_WithDifferentStoredCredentialInitiators_ShouldMapCorrectly(string initiator)
    {
        // Arrange
        var request = new CreateTokenRequest
        {
            Pan = "4111111111111111",
            Network = "Visa",
            ExpirationMonth = 12,
            ExpirationYear = 2025,
            CardholderName = "John Doe",
            CustomerId = "customer-456",
            PaymentMethodType = "Card",
            TokenType = "OneTime",
            StoredCredentialInitiator = initiator
        };

        // Act
        var command = _mapper.MapRequest(request);

        // Assert
        command.StoredCredentialInitiator.Should().Be(initiator);
    }

    [Theory]
    [InlineData("Recurring")]
    [InlineData("Unscheduled")]
    public void MapRequest_WithDifferentStoredCredentialReasons_ShouldMapCorrectly(string reason)
    {
        // Arrange
        var request = new CreateTokenRequest
        {
            Pan = "4111111111111111",
            Network = "Visa",
            ExpirationMonth = 12,
            ExpirationYear = 2025,
            CardholderName = "John Doe",
            CustomerId = "customer-456",
            PaymentMethodType = "Card",
            TokenType = "OneTime",
            StoredCredentialReason = reason
        };

        // Act
        var command = _mapper.MapRequest(request);

        // Assert
        command.StoredCredentialReason.Should().Be(reason);
    }

    [Fact]
    public void MapRequest_WithLongCardholderName_ShouldMapCorrectly()
    {
        // Arrange
        var longName = new string('A', 100); // Maximum length
        var request = new CreateTokenRequest
        {
            Pan = "4111111111111111",
            Network = "Visa",
            ExpirationMonth = 12,
            ExpirationYear = 2025,
            CardholderName = longName,
            CustomerId = "customer-456",
            PaymentMethodType = "Card",
            TokenType = "OneTime"
        };

        // Act
        var command = _mapper.MapRequest(request);

        // Assert
        command.Card.Should().NotBeNull();
        command.Card.CardholderName.Should().Be(longName);
    }

    [Fact]
    public void MapRequest_WithSpecialCharactersInCardholderName_ShouldMapCorrectly()
    {
        // Arrange
        const string specialName = "José María O'Connor-Smith";
        var request = new CreateTokenRequest
        {
            Pan = "4111111111111111",
            Network = "Visa",
            ExpirationMonth = 12,
            ExpirationYear = 2025,
            CardholderName = specialName,
            CustomerId = "customer-456",
            PaymentMethodType = "Card",
            TokenType = "OneTime"
        };

        // Act
        var command = _mapper.MapRequest(request);

        // Assert
        command.Card.Should().NotBeNull();
        command.Card.CardholderName.Should().Be(specialName);
    }

    [Fact]
    public void MapResponse_WithNullOptionalValue_ShouldMapCorrectly()
    {
        // Arrange
        var summary = TestTokenSummary.Valid() with { Network = null };

        // Act
        var response = _mapper.MapResponse(summary);

        // Assert
        response.Network.Should().BeNull();
    }
}
