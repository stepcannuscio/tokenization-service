namespace Tokenization.Domain.ValueObjects;

/// <summary>
/// Describes a specific KEK version in use.
/// </summary>
internal sealed record KeyVersionInfo
{
    /// <summary>
    /// Fully-qualified KEK identifier (ideally includes version) that wrapped the DEK.
    /// Store this for fast exact-version unwraps.
    /// </summary>
    public required string KekKeyId { get; init; }

    /// <summary>
    /// UTC timestamp of when this KEK version was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether the KEK version is the current version used for wrapping.
    /// </summary>
    public bool IsCurrent { get; init; }
}
