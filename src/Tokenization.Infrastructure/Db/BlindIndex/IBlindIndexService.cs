namespace Tokenization.Infrastructure.Db.BlindIndex;

/// <summary>
/// Computes deterministic, non-reversible blind indexes (HMAC-SHA256) for equality searches.
/// Keep the index key material separate from encryption keys and rotate via a key id.
/// </summary>
internal interface IBlindIndexService
{
    /// <summary>
    /// Computes a blind-index hash for the supplied value using a specific index key id.
    /// </summary>
    /// <param name="value">The plaintext value to index (e.g., TenantId).</param>
    /// <param name="keyId">Optional rotation key id; must match the id used at insert time (default "v1").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>32-byte HMAC-SHA256 hash, or <c>null</c> if <paramref name="value"/> is null/empty.</returns>
    Task<byte[]> ComputeAsync(string value, string? keyId, CancellationToken ct = default);
}