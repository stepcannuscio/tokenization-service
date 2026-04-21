using Tokenization.Domain.Enums;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Tests.Shared.Utils.ValueObjects;

internal static class TestCreateTokenArgs
{
    public static CreateTokenArgs Valid(string? token = null) =>
        new(
            Token: token,
            MaskedData: "**********1111",
            Last4: "1111",
            PaymentMethodType: PaymentMethodType.Card,
            Network: "visa",
            PaymentMethodMetadata: "{\"brand\":\"visa\"}",
            Currency: "USD",
            Country: "US",
            TenantId: "tenant-123",
            CustomerId: "customer-789",
            TokenType: TokenType.OneTime,
            MaxUses: 5,
            InitialTransactionId: "test-transaction-id",
            StoredCredentialInitiator: StoredCredentialInitiator.Merchant,
            StoredCredentialReason: StoredCredentialReason.Unscheduled,
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(30));
}
