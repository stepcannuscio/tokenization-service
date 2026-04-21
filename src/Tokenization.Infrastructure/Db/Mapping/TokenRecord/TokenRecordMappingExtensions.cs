using Tokenization.Domain.Entities;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Infrastructure.Db.Mapping.TokenRecord;

/// <summary>
/// Convenience extension methods that delegate to the strongly-typed token mappers.
/// </summary>
internal static class TokenRecordMappingExtensions
{
    /// <summary>
    /// Maps a <see cref="CreateTokenArgs"/> + <see cref="EncryptedPayload"/> to a new <see cref="TokenRecord"/>.
    /// </summary>
    public static Domain.Entities.TokenRecord ToTokenRecord(
        this CreateTokenArgs args,
        EncryptedPayload encrypted,
        string? tokenOverride = null,
        DateTimeOffset? nowUtc = null) =>
        CreateTokenArgsToTokenRecordMapper.Map(args, encrypted, tokenOverride, nowUtc);

    /// <summary>
    /// Maps a <see cref="TokenRecord"/> to a non-sensitive <see cref="TokenSummary"/>.
    /// </summary>
    public static TokenSummary ToSummary(this Domain.Entities.TokenRecord source) =>
        TokenRecordSummaryMapper.Map(source);

    /// <summary>
    /// Maps a <see cref="TokenRecord"/> to a <see cref="TokenUsageResult"/>.
    /// </summary>
    public static TokenUsageResult ToUsageResult(this Domain.Entities.TokenRecord source) =>
        TokenRecordToUsageResultMapper.Map(source);
}