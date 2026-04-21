using Tokenization.Application.Handlers.CreateToken;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Tests.Shared.Utils.Commands;

internal static class TestCreateTokenCommand
{
    public static CreateTokenCommand Valid()
    {
        return new CreateTokenCommand
        {
            TenantId = "tenant-123",
            CustomerId = "customer-123",
            PaymentMethodType = "Card",
            TokenType = "OneTime",
            Card = new CardPlaintext
            {
                Pan = "4111111111111111",
                ExpMonth = 1,
                ExpYear = DateTime.Now.Year + 5
            }
        };
    }
}
