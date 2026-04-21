namespace Tokenization.Api.Responses;

/// <summary>
/// Represents the response from a token creation request, containing the tokenized payment method details.
/// </summary>
public sealed record CreateTokenResponse
{
    /// <summary>
    /// The tokenized payment method identifier that replaces the sensitive PAN data.
    /// This token can be used for subsequent payment operations without exposing sensitive card data.
    /// </summary>
    public string Token { get; init; } = null!;

    /// <summary>
    /// The masked representation of the card data for display purposes (e.g., "4111********1111").
    /// This provides a secure way to show card information to users without exposing sensitive data.
    /// </summary>
    public string MaskedData { get; init; } = null!;

    /// <summary>
    /// The last four digits of the original card number for identification purposes.
    /// </summary>
    public string? Last4 { get; init; }

    /// <summary>
    /// The type of payment method that was tokenized (e.g., "CreditCard", "DebitCard").
    /// </summary>
    public string PaymentMethodType { get; init; } = null!;

    /// <summary>
    /// The payment network of the tokenized card (e.g., "Visa", "Mastercard", "American Express").
    /// May be null if the network could not be determined during tokenization.
    /// </summary>
    public string? Network { get; init; }
}
