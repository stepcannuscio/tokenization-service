using FluentAssertions;
using System.Text;
using System.Text.RegularExpressions;
using Tokenization.Application.Handlers.CreateToken;
using Tokenization.Domain.Enums;
using Tokenization.Domain.ValueObjects;
using Xunit;

namespace Tokenization.Tests.Unit.Application.Handlers.CreateToken;

public partial class CreateTokenMapperTests
{
    [Fact]
    public void ToCreateTokenArgs_BuildsMaskedAndLast4_AndMapsEnums()
    {
        var cmd = new CreateTokenCommand
        {
            TenantId = "tenant-123",
            CustomerId = "customer-123",
            PaymentMethodType = "Card",
            TokenType = "ONETIME",
            StoredCredentialInitiator = "customer",
            StoredCredentialReason = "recurring",
            Network = "Visa",
            Currency = "USD",
            Country = "US",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            MaxUses = 5,
            InitialTransactionId = "txn_001",
            Card = new CardPlaintext
            {
                Pan = "4111111111111111",
                ExpMonth = 2,
                ExpYear = 2031,
                CardholderName = "Ada Lovelace"
            }
        };

        var args = cmd.ToCreateTokenArgs();

        args.MaskedData.Should().EndWith("1111");
        args.Last4.Should().Be("1111");
        args.MaskedData.Should().MatchRegex(TestMaskedDataRegex());
        args.PaymentMethodType.Should().Be(PaymentMethodType.Card);
        args.StoredCredentialInitiator.Should().Be(StoredCredentialInitiator.Customer);
        args.StoredCredentialReason.Should().Be(StoredCredentialReason.Recurring);
        args.TokenType.Should().Be(TokenType.OneTime);
        args.TenantId.Should().Be(cmd.TenantId);
        args.CustomerId.Should().Be(cmd.CustomerId);
        args.Network.Should().Be(cmd.Network);
        args.Currency.Should().Be(cmd.Currency);
        args.Country.Should().Be(cmd.Country);
        args.MaxUses.Should().Be(cmd.MaxUses);
        args.InitialTransactionId.Should().Be(cmd.InitialTransactionId);
        args.ExpiresAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData("5", 1, 2030, "", "card|5|01|2030|")]
    [InlineData("4111111111111111", 10, 2035, "Ada", "card|4111111111111111|10|2035|Ada")]
    public void ToSensitivePayload_FormatsPipeDelimited(string pan, int mm, int yyyy, string? name, string expected)
    {
        var cmd = new CreateTokenCommand
        {
            TenantId = "m",
            PaymentMethodType = "Card",
            StoredCredentialInitiator = "Customer",
            Card = new CardPlaintext
            {
                Pan = pan,
                ExpMonth = mm,
                ExpYear = yyyy,
                CardholderName = name
            }
        };

        var bytes = cmd.ToSensitivePayload();
        Encoding.UTF8.GetString(bytes.ToArray()).Should().Be(expected);
    }

    [GeneratedRegex(@".\b(?=\w{4}$)")]
    private static partial Regex TestMaskedDataRegex();
}