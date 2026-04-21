using Tokenization.Domain.ValueObjects;

namespace Tokenization.Domain.Abstractions;

/// <summary>
/// Abstraction over a Key Encryption Key (KEK) management system.
/// Implementations must:
/// <list type="bullet">
///   <item><description>Wrap/unwrap DEKs WITHOUT exposing raw KEK material in production.</description></item>
///   <item><description>Support KEK rotation and decryption fallback across versions.</description></item>
///   <item><description>Be thread-safe for concurrent use.</description></item>
///</list>
/// </summary>
/// <remarks>
///   For HSM-backed providers (e.g., Azure Key Vault), only server-side wrap/unwrap operations
///   should be performed - KEK bytes must never be exported. Callers should persist the returned <see cref="KeyWrapPayload.KekKeyId"/>
///   alongside the wrapped DEK for efficient exact-version unwraps.
/// </remarks>
internal interface IKeyProvider
{
    /// <summary>
    /// Wraps (encrypts) a DEK use an active KEK. Implementations should first attempt to wrap using the current KEK and
    /// fallback to other known active versions if needed.
    /// </summary>
    /// <param name="dek">Plaintext DEK bytes (caller owns zeroing after use).</param>
    /// <param name="keyName">Logical key name (e.g., "payment-kek") used to select the key client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="KeyWrapPayload"/> containing the wrapped DEK and the fully-qualified KEK identifier.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when no active KEK is available.</exception>
    Task<KeyWrapPayload> WrapKeyAsync(byte[] dek, string keyName, CancellationToken ct = default);

    /// <summary>
    /// Unwraps (decrypts) a previously wrapped DEK. If <paramref name="keyId"/> is provided, implementations should
    /// attempt that exact KEK id first and fallback to other known active versions if needed.
    /// </summary>
    /// <param name="wrappedDek">Wrapped DEK bytes.</param>
    /// <param name="keyName">Logical key name (e.g., "payment-kek") used to select the key client.</param>
    /// <param name="keyId">Optional fully-qualified KEK identifier to use as the first attempt.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The plaintext DEK bytes (caller must zero after use).
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when no active KEK could unwrap the DEK.</exception>
    Task<byte[]> UnwrapKeyAsync(byte[] wrappedDek, string keyName, string? keyId, CancellationToken ct = default);

    /// <summary>
    /// Proactively creates a new KEK version (if the backing KMS supports programmatic rotation)
    /// and makes it the current key.
    /// </summary>
    /// <param name="keyName">Logical key name (e.g., "payment-kek") used to select the key client.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RotateKeyAsync(string keyName, CancellationToken ct = default);

    /// <summary>
    /// Reloads the in-process cache with keys from the backing KMS.
    /// Useful when rotation happens outside the process (e.g., via portal/ops pipeline) or via Event Grid.
    /// </summary>
    /// <param name="keyName">Logical key name (e.g., "payment-kek") used to select the key client.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ReloadKeysAsync(string keyName, CancellationToken ct = default);

    /// <summary>
    /// Preloads all active key versions.
    /// Call at startup for warm paths and lower latency on first use.
    /// </summary>
    /// <param name="keyName">Logical key name (e.g., "payment-kek") used to select the key client.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PreloadKeysAsync(string keyName, CancellationToken ct = default);
    
    /// <summary>
    /// Signs data using an active HSM key. Implementations should first attempt to sign data using the current key and
    /// fallback to other known active versions if needed.
    /// </summary>
    /// <param name="data">Plaintext data bytes.</param>
    /// <param name="keyName">Logical key name (e.g., "payment-kek") used to select the key client.</param>
    /// <param name="keyId">Optional fully-qualified key identifier to use as the first attempt.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A cryptographically signed byte array of the data. 
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when no active key is available.</exception>
    Task<byte[]> SignDataAsync(byte[] data, string keyName, string? keyId, CancellationToken ct = default);
}