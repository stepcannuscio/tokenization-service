using Tokenization.Domain.Entities;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Infrastructure.Db.Mapping.TokenRecord;

/// <summary>
/// Maps <see cref="CreateTokenArgs"/> (non-sensitive inputs) plus an <see cref="EncryptedPayload"/>
/// to a fully populated <see cref="TokenRecord"/> ready to persist.
/// </summary>
internal sealed class CreateTokenArgsToTokenRecordMapper
{
    /// <summary>
    /// Performs the mapping to <see cref="TokenRecord"/>.
    /// </summary>
    /// <param name="args">Creation arguments (no plaintext PAN).</param>
    /// <param name="encrypted">Encryption envelope for the PCI payload.</param>
    /// <param name="tokenOverride">Optional token value. If null, uses <paramref name="args"/>.Token or the DB generator.</param>
    /// <param name="nowUtc">Optional clock override for tests; defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    /// <returns>A configured <see cref="TokenRecord"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> or <paramref name="encrypted"/> is <c>null</c>.</exception>
    public static Domain.Entities.TokenRecord Map(
        CreateTokenArgs args,
        EncryptedPayload encrypted,
        string? tokenOverride = null,
        DateTimeOffset? nowUtc = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(encrypted);

        var now = nowUtc ?? DateTimeOffset.UtcNow;

        return new Domain.Entities.TokenRecord
        {
            // External key
            Token = tokenOverride ?? args.Token ?? Guid.NewGuid().ToString("N"),

            // Non-sensitive display/metadata
            MaskedData = args.MaskedData,
            Last4 = args.Last4,
            PaymentMethodType = args.PaymentMethodType,
            Network = args.Network,
            PaymentMethodMetadata = args.PaymentMethodMetadata,
            Currency = args.Currency,
            Country = args.Country,
            TenantId = args.TenantId,
            CustomerId = args.CustomerId,
            InitialTransactionId = args.InitialTransactionId ?? Guid.NewGuid().ToString("N"),

            // Lifecycle / usage
            TokenType = args.TokenType,
            MaxUses = args.MaxUses,
            UsageCount = 0,
            IsActive = true,
            CreatedAt = now,
            LastUsedAt = null,
            ExpiresAt = args.ExpiresAt,
            StoredCredentialInitiator = args.StoredCredentialInitiator,
            StoredCredentialReason = args.StoredCredentialReason,

            // PCI envelope (never indexed)
            EncryptedPayload = encrypted
        };
    }
}
