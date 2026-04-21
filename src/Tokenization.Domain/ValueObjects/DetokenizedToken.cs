using Tokenization.Domain.Entities;

namespace Tokenization.Domain.ValueObjects;

/// <summary>
/// Sensitive projection of PCI-data stored in a <see cref="TokenRecord"/>.
/// </summary>
internal record DetokenizedToken(string Plaintext, TokenSummary TokenSummary);

internal static class DetokenizedTokenExtensions
{
    public static CardPlaintext ToCardPlaintext(this DetokenizedToken detokenizedToken)
    {
        ArgumentNullException.ThrowIfNull(detokenizedToken);
        
        var data = detokenizedToken.Plaintext.Split("|", StringSplitOptions.RemoveEmptyEntries);
        
        var pan = data.Length > 1 ? data[1] : string.Empty;
        var expMonth = data.Length > 2 ? int.TryParse(data[2], out var month) ? month : 0 : 0;
        var expYear = data.Length > 3 ? int.TryParse(data[3], out var year) ? year : 0 : 0;
        var cardholderName = data.Length > 4 ? data[4] : null;

        return new CardPlaintext
        {
            Pan = pan,
            ExpMonth = expMonth,
            ExpYear = expYear,
            CardholderName = cardholderName
        };
    }
}