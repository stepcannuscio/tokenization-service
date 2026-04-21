using Tokenization.Domain.Entities;
using Tokenization.Domain.Enums;

namespace Tokenization.Domain.ValueObjects;

/// <summary>
/// Non-sensitive projection of <see cref="TokenRecord"/> for external callers.
/// </summary>
internal record TokenSummary(
    string Token,
    string MaskedData,
    string? Last4,
    PaymentMethodType PaymentMethodType,
    string? Network,
    string? Currency,
    string? Country,
    string TenantId,
    string CustomerId,
    TokenType TokenType,
    int UsageCount,
    int? MaxUses,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? ExpiresAt);

internal static class TokenSummaryExtensions
{
    public static bool IsExpired(this TokenSummary summary, DateTimeOffset? nowUtc)
    {
        return summary.ExpiresAt is not null && summary.ExpiresAt < (nowUtc ?? DateTimeOffset.UtcNow);
    }
    
    public static bool IsUsageExceeded(this TokenSummary summary)
    {
        return summary.MaxUses.HasValue && summary.UsageCount >= summary.MaxUses;
    }
} 