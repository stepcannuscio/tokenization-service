namespace Tokenization.Api.Responses;

/// <summary>
/// Response containing token information.
/// </summary>
public sealed record GetTokenResponse
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

    /// <summary>
    /// The customer ID associated with the token.
    /// </summary>
    public string CustomerId { get; init; } = null!;

    /// <summary>
    /// The tenant ID that owns the token.
    /// </summary>
    public string TenantId { get; init; } = null!;

    /// <summary>
    /// The date and time when the token was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The date and time when the token expires (if applicable).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// The maximum number of uses allowed for the token (if applicable).
    /// </summary>
    public int? MaxUses { get; init; }

    /// <summary>
    /// The current number of times the token has been used.
    /// </summary>
    public int? UsageCount { get; init; }
}
