namespace Tokenization.Domain.ValueObjects;

/// <summary>
/// Result of wrapping a freshly generated DEK with a KEK.
/// Transient value object used by callers to persist the wrapped DEK and metadata.
/// </summary>
internal sealed record KeyWrapPayload
{
    /// <summary>
    /// Wrapped DEK bytes returned by the provider/KMS.
    /// </summary>
    public required byte[] WrappedDek { get; init; }

    /// <summary>
    /// Fully-qualified KEK identifier (ideally includes version) that wrapped the DEK.
    /// Store this for fast exact-version unwraps.
    /// </summary>
    public required string KekKeyId { get; init; }

    /// <summary>
    /// Key wrap algorithm used (e.g., "RSA-OAEP-256").
    /// </summary>
    public string Algorithm { get; init; } = "RSA-OAEP-256";

    /// <summary>
    /// UTC timestamp of when the wrap occurred (for audit/debug).
    /// </summary>
    public DateTimeOffset WrappedAt { get; init; } = DateTimeOffset.UtcNow;
}
