namespace Tokenization.Api.Responses;

/// <summary>
/// Represents the response from a detokenize token request.
/// Contains PCI Data so must only be handled within a PCI-Scope.
/// </summary>
public sealed record DetokenizeTokenResponse
{
    /// <summary>
    /// Primary account number (PAN), numeric only.
    /// </summary>
    public string Pan { get; set; } = null!;

    /// <summary>
    /// One or two-digit month (1..12).
    /// </summary>
    public int ExpMonth { get; set; }

    /// <summary>
    /// Four-digit year.
    /// </summary>
    public int ExpYear { get; set; }

    /// <summary>
    /// Cardholder name.
    /// </summary>
    public string? CardholderName { get; set; }

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