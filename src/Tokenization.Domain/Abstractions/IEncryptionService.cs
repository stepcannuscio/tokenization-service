using System.Security.Cryptography;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Domain.Abstractions;

/// <summary>
/// Performs per-record authenticated encryption using ephemeral DEKs wrapped by KEKs.
/// Implementations must:
/// <list type="bullet">
///   <item><description>Generate a fresh DEK for each <c>EncryptAsync</c> call.</description></item>
///   <item><description>Use an authenticated encryption mechanism.</description></item>
///   <item><description>Wrap the DEK via <c>IKeyProvider</c> and return the encrypted payload.</description></item>
///   <item><description>Zero DEK bytes as soon as feasible.</description></item>
/// </list>
/// </summary>
internal interface IEncryptionService
{
    /// <summary>
    /// Encrypts the provided <paramref name="plaintext"/> using a freshly-generated DEK and
    /// wraps that DEK using the <c>IKeyProvider</c>.
    /// </summary>
    /// <param name="plaintext">UTF-8 string to encrypt.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="EncryptedPayload"/> containing necessary data for eventually deciphering the ciphertext.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="plaintext"/> is null or whitespace</exception>
    /// <exception cref="CryptographicException">Thrown if the encryption fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the <c>IKeyProvider</c> fails to wrap the DEK.</exception>
    Task<EncryptedPayload> EncryptAsync(string plaintext, CancellationToken ct = default);

    /// <summary>
    /// Decrypts the provided <paramref name="payload"/> by using the <c>IKeyProvider</c>.
    /// </summary>
    /// <param name="payload">Encrypted payload previously returned by <see cref="EncryptAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The original plaintext string (UTF-8).</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="payload"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the <c>IKeyProvider</c> fails to unwrap the DEK.</exception>
    Task<string> DecryptAsync(EncryptedPayload payload, CancellationToken ct = default);
}
