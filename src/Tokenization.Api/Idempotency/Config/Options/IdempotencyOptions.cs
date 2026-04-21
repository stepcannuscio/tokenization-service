namespace Tokenization.Api.Idempotency.Config.Options;

/// <summary>
/// Configuration options for the idempotency middleware.
/// </summary>
internal sealed class IdempotencyOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Idempotency";

    /// <summary>
    /// Gets or sets the time-to-live (TTL) in seconds for cached idempotent responses.
    /// After this duration, cached responses will expire and new requests with the same
    /// idempotency key will be processed normally.
    /// </summary>
    /// <value>The TTL in seconds. Default is 600 seconds (10 minutes).</value>
    public int TtlSeconds { get; init; } = 600; // 10 minutes default
}
