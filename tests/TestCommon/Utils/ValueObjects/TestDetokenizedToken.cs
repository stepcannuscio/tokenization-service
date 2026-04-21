using Tokenization.Domain.ValueObjects;

namespace Tokenization.Tests.Shared.Utils.ValueObjects;

internal static class TestDetokenizedToken
{
    public static DetokenizedToken Valid()
    {
        return new DetokenizedToken(
            Plaintext: "card|4111111111111111|10|2035|Ada",
            TokenSummary: TestTokenSummary.Valid());
    }
}
