using Tokenization.Domain.Entities;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Infrastructure.Db.Mapping.TokenRecord;

/// <summary>
/// Maps a <see cref="TokenRecord"/> to a lightweight <see cref="TokenUsageResult"/> summary after usage updates.
/// </summary>
internal sealed class TokenRecordToUsageResultMapper
{
    /// <summary>
    /// Builds a <see cref="TokenUsageResult"/> from the provided <see cref="TokenRecord"/>.
    /// </summary>
    /// <param name="source">Source Token record.</param>
    /// <returns>A populated <see cref="TokenUsageResult"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
    public static TokenUsageResult Map(Domain.Entities.TokenRecord source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new TokenUsageResult(
            Token: source.Token,
            UsageCount: source.UsageCount,
            MaxUses: source.MaxUses,
            IsActive: source.IsActive,
            LastUsedAt: source.LastUsedAt
        );
    }
}
