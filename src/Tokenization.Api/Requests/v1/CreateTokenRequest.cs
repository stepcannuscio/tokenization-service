using System.ComponentModel.DataAnnotations;
using Tokenization.Api.Logging;

namespace Tokenization.Api.Requests.v1;

/// <summary>
/// Represents a request to create a tokenized payment method from sensitive card data.
/// This class contains sensitive payment information that will be tokenized for secure storage and processing.
/// </summary>
public sealed record CreateTokenRequest
{
    /// <summary>
    /// The Primary Account Number (PAN) - the sensitive card number that will be tokenized.
    /// Must be between 12 and 19 digits in length.
    /// </summary>
    [Required]
    [Sensitive(Sensitivity.Payment)]
    public required string Pan { get; init; }

    /// <summary>
    /// The card expiration month (1-12).
    /// </summary>
    [Required]
    public int ExpirationMonth { get; init; }

    /// <summary>
    /// The card expiration year (2000-2100).
    /// </summary>
    [Required]
    public int ExpirationYear { get; init; }

    /// <summary>
    /// The name of the cardholder as it appears on the card.
    /// Maximum length is 100 characters.
    /// </summary>
    [Required]
    [Sensitive(Sensitivity.Pii)]
    public required string CardholderName { get; init; }

    /// <summary>
    /// The payment network (e.g., "Visa", "Mastercard", "American Express").
    /// </summary>
    [Required]
    public required string Network { get; init; }

    /// <summary>
    /// The unique identifier of the customer for whom the token is being created.
    /// </summary>
    [Required]
    public required string CustomerId { get; init; }

    /// <summary>
    /// The type of payment method (e.g., "Card").
    /// </summary>
    [Required]
    public required string PaymentMethodType { get; init; }

    /// <summary>
    /// Token type (e.g., OneTime, StoredCredential).
    /// </summary>
    [Required]
    public required string TokenType { get; init; }

    /// <summary>
    /// Identifies who initiated the use of a stored credential (e.g., Customer, Merchant).
    /// </summary>
    public string? StoredCredentialInitiator { get; init; }

    /// <summary>
    /// Identifies the reason for the stored credential (e.g., Recurring, Installments).
    /// </summary>
    public string? StoredCredentialReason { get; init; }

    /// <summary>
    /// ISO 4217 currency.
    /// </summary>
    public string? Currency { get; init; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country.</summary>
    public string? Country { get; init; }

    /// <summary>
    /// Token expiry.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Max usages for one-time/semi-limited tokens.
    /// </summary>
    public int? MaxUses { get; init; }

    /// <summary>
    /// Initial transaction id.
    /// </summary>
    public string? InitialTransactionId { get; init; }
}
