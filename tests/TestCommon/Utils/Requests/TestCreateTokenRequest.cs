using Tokenization.Api.Requests.v1;

namespace Tokenization.Tests.Shared.Utils.Requests;

internal static class TestCreateTokenRequest
{
    public static CreateTokenRequest Valid()
    {
        var futureDate = DateTime.UtcNow.AddYears(2);

        return new CreateTokenRequest
        {
            Pan = "4111111111111111",
            ExpirationMonth = futureDate.Month,
            ExpirationYear = futureDate.Year,
            CardholderName = "John Doe",
            Network = "Visa",
            CustomerId = Guid.NewGuid().ToString(),
            PaymentMethodType = "Card",
            TokenType = "OneTime"
        };
    }
}
