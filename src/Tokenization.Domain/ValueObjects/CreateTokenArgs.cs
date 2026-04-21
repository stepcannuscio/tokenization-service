using Tokenization.Domain.Enums;

namespace Tokenization.Domain.ValueObjects;

/// <summary>
/// Arguments for creating a token. All properties are non-sensitive.
/// </summary>
internal sealed record CreateTokenArgs(
    string? Token,
    string MaskedData,
    string? Last4,
    PaymentMethodType PaymentMethodType,
    string? Network,
    string? PaymentMethodMetadata,
    string? Currency,
    string? Country,
    string TenantId,
    string CustomerId,
    TokenType TokenType,
    int? MaxUses,
    string? InitialTransactionId,
    StoredCredentialInitiator? StoredCredentialInitiator,
    StoredCredentialReason? StoredCredentialReason,
    DateTimeOffset? ExpiresAt);
