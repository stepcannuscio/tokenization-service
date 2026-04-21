namespace Tokenization.Domain.ValueObjects;

/// <summary>
/// Result of a usage increment operation.
/// </summary>
internal sealed record TokenUsageResult(
    string Token,
    int UsageCount,
    int? MaxUses,
    bool IsActive,
    DateTimeOffset? LastUsedAt);

internal static class TokenUsageResultExtensions
{
    public static bool IsUsageExceeded(this TokenUsageResult result)
    {
        return result.MaxUses.HasValue && result.UsageCount >= result.MaxUses;
    }
}