using Tokenization.Api.Requests.v1;

namespace Tokenization.Tests.Shared.Utils.Requests;

internal static class TestCreateTokenRequest
{
    public static CreateTokenRequest Valid()
    {
        return new CreateTokenRequest
        {
            Pan = "4111111111111111",
            ExpirationMonth = 12,
            ExpirationYear = 2025,
            CardholderName = "John Doe",
            Network = "Visa",
            CustomerId = Guid.NewGuid().ToString(),
            PaymentMethodType = "Card",
            TokenType = "OneTime"
        };
    }
}