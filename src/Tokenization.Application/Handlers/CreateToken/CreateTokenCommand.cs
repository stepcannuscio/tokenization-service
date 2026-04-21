using MediatR;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Application.Handlers.CreateToken;

/// <summary>
/// Command to tokenize a payment method from plaintext card data.
/// </summary>
internal sealed class CreateTokenCommand : IRequest<TokenSummary>
{
    /// <summary>Tenant identifier.</summary>
    public string TenantId { get; set; } = null!;

    /// <summary>Customer identifier.</summary>
    public string CustomerId { get; set; } = null!;

    /// <summary>Payment method type (e.g., Card, ApplePay).</summary>
    public string PaymentMethodType { get; set; } = null!;

    /// <summary>Token type (e.g., OneTime, StoredCredential).</summary>
    public string TokenType { get; set; } = null!;

    /// <summary>Identifies who initiated the use of a stored credential (e.g., Customer, Merchant).</summary>
    public string? StoredCredentialInitiator { get; set; }

    /// <summary>Identifies the reason for the stored credential (e.g., Recurring, Installments).</summary>
    public string? StoredCredentialReason { get; set; }

    /// <summary>Network/brand (e.g., Visa).</summary>
    public string? Network { get; set; }

    /// <summary>ISO 4217 currency (optional).</summary>
    public string? Currency { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country (optional).</summary>
    public string? Country { get; set; }

    /// <summary>Optional token expiry.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Optional max usages for one-time/semi-limited tokens.</summary>
    public int? MaxUses { get; set; }

    /// <summary>Initial transaction id (optional; stored for audit/COF frameworks).</summary>
    public string? InitialTransactionId { get; set; }

    /// <summary>Plaintext card input. Never log or echo these fields.</summary>
    public CardPlaintext? Card { get; set; }
}
