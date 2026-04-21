using Tokenization.Domain.Entities;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Infrastructure.Db.Mapping.TokenRecord;

/// <summary>
/// Maps a persisted <see cref="TokenRecord"/> to a non-sensitive <see cref="TokenSummary"/>.
/// </summary>
internal sealed class TokenRecordSummaryMapper
{
    /// <summary>
    /// Creates a projection-friendly <see cref="TokenSummary"/> from <see cref="TokenRecord"/>.
    /// </summary>
    /// <param name="source">Source Token record.</param>
    /// <returns>A populated <see cref="TokenSummary"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
    public static TokenSummary Map(Domain.Entities.TokenRecord source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new TokenSummary(
            Token: source.Token,
            MaskedData: source.MaskedData,
            Last4: source.Last4,
            PaymentMethodType: source.PaymentMethodType,
            Network: source.Network,
            Currency: source.Currency,
            Country: source.Country,
            TenantId: source.TenantId,
            CustomerId: source.CustomerId,
            UsageCount: source.UsageCount,
            TokenType: source.TokenType,
            MaxUses: source.MaxUses,
            IsActive: source.IsActive,
            CreatedAt: source.CreatedAt,
            LastUsedAt: source.LastUsedAt,
            ExpiresAt: source.ExpiresAt
        );
    }
}
