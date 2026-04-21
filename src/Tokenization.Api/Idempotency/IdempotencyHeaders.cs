namespace Tokenization.Api.Idempotency;

/// <summary>
/// Defines the headers used for Idempotent requests.
/// </summary>
internal static class IdempotencyHeaders
{
    /// <summary>
    /// The header used by the client to differentiate requests.
    /// </summary>
    public const string IdempotencyKey = "Idempotency-Key";

    /// <summary>
    /// The header returned to indicate a response has been replayed.
    /// </summary>
    public const string IdempotencyReplay = "Idempotency-Replay";
}
