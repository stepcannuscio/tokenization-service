using Tokenization.Domain.Enums;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Domain.Entities;

/// <summary>
/// Represents a stored mapping between a token and the underlying payment method.
/// Supports card and alternative payment methods (APMs). 
/// Lives entirely in PCI-scoped storage; must never expose raw PAN or sensitive APM credentials outside PCI scope.
/// </summary>
internal sealed class TokenRecord
{
    /// <summary>Primary key</summary>
    public long Id { get; init; }  
    
    /// <summary>
    /// The surrogate token that replaces the raw payment credential in non-PCI systems.
    /// Safe to share outside the PCI environment.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// The encrypted payload for this token. 
    /// Contains the <c>KeyId</c> (which identifies the key version used),
    /// the AES-GCM <c>Nonce</c> and <c>Tag</c>, and the actual <c>Ciphertext</c>.
    /// 
    /// This value object encapsulates all cryptographic material required to
    /// safely decrypt the token, except for the key itself which is resolved
    /// via the <see cref="Domain.Abstractions.IKeyProvider"/>.
    /// </summary>
    public required EncryptedPayload EncryptedPayload { get; init; }

    /// <summary>
    /// Human-readable masked representation for display (e.g., "**** **** **** 1234", masked email or bank account).
    /// Safe for UI, logs, and receipts.
    /// </summary>
    public required string MaskedData { get; init; }

    /// <summary>
    /// Last four digits of the identifier (PAN, account number, or APM ID) for display or filtering.
    /// </summary>
    public string? Last4 { get; init; }

    /// <summary>
    /// Identifies the payment method type (Card, GooglePay, ApplePay, Alipay, Boleto, etc.).
    /// Determines processing flow in the gateway.
    /// </summary>
    public required PaymentMethodType PaymentMethodType { get; init; }

    /// <summary>
    /// Card brand or APM provider/network (Visa, Mastercard, AlipayCN, etc.).
    /// </summary>
    public string? Network { get; init; }
    
    /// <summary>
    /// JSON or serialized metadata for payment-method-specific fields.
    /// Example: Apple Pay cryptogram, Alipay account reference, bank code for Boleto.
    /// </summary>
    public string? PaymentMethodMetadata { get; init; }

    /// <summary>
    /// ISO 4217 three-letter currency code relevant to this payment method.
    /// Useful for alternative payment methods with currency restrictions.
    /// </summary>
    public string? Currency { get; init; }

    /// <summary>
    /// ISO 3166 two-letter country code relevant to this payment method.
    /// Helpful for regulatory and routing rules.
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// Identifier of the tenant that owns this token.
    /// Required to ensure tokens cannot be misused across tenants.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Customer identifier associated with the token.
    /// Required to ensure tokens cannot be misused across customers.
    /// </summary>
    public required string CustomerId { get; init; }
        
    /// <summary>
    /// Optional reference to the initial transaction that created or credentialed this token.
    /// Required for tenant-initiated transactions (MIT).
    /// </summary>
    public string? InitialTransactionId { get; init; }
        
    /// <summary>
    /// The type of token lifecycle: one-time, stored-credential
    /// </summary>
    public required TokenType TokenType { get; init; }
    
    /// <summary>
    /// Indicates who initiated a stored credential: customer or processor-side merchant.
    /// Used to generate proper stored-credential flags for processors.
    /// </summary>
    public StoredCredentialInitiator? StoredCredentialInitiator { get; init; }

    /// <summary>
    /// Reason for the stored credential, relevant for recurring payments, installments, or unscheduled charges.
    /// </summary>
    public StoredCredentialReason? StoredCredentialReason { get; init; }

    /// <summary>
    /// Optional maximum number of times this token can be used.
    /// Null means unlimited usage.
    /// </summary>
    public int? MaxUses { get; init; }

    /// <summary>
    /// Current number of times this token has been used.
    /// Useful for enforcing single-use tokens or usage limits.
    /// </summary>
    public int UsageCount { get; set; }
    
    /// <summary>
    /// Whether the token is currently active and available for use.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Timestamp when the token was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        
    /// <summary>
    /// Timestamp when the token was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp of the last transaction that used this token.
    /// Useful for lifecycle management and cleanup.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }
    
    /// <summary>
    /// Expiration date of the payment method (if applicable, e.g., for cards).
    /// Stored encrypted in PCI scope.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}
