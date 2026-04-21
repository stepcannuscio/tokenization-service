using Tokenization.Domain.Enums;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Tests.Shared.Utils.ValueObjects;

internal static class TestTokenSummary
{
    public static TokenSummary Valid()
    {
        return new TokenSummary(
            Token: "tok_123",
            MaskedData: "************1111",
            Last4: "1111",
            PaymentMethodType: PaymentMethodType.Card,
            Network: "Visa",
            Currency: "USD",
            Country: "US",
            TenantId: "tenant-123",
            CustomerId: "customer-123",
            TokenType: TokenType.OneTime,
            MaxUses: 5,
            UsageCount: 1,
            IsActive: true,
            CreatedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
            LastUsedAt: null,
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(10));
    }
}
