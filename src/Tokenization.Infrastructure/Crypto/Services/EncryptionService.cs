using System.Security.Cryptography;
using System.Text;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Infrastructure.Crypto.Services;

/// <summary>
/// Default AES-GCM implementation that uses per-record DEKs wrapped by KEKs via <see cref="IKeyProvider"/>.
/// </summary>
internal sealed class EncryptionService(IKeyProvider keyProvider, string keyName) : IEncryptionService
{
    // AES-GCM parameters
    private const int DekSizeBytes = 32; // 256-bit DEK
    private const int NonceSizeBytes = 12; // Recommended GCM nonce size
    private const int TagSizeBytes = 16; // 128-bit authentication tag

    /// <inheritdoc />
    public async Task<EncryptedPayload> EncryptAsync(string plaintext, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(plaintext)) throw new ArgumentException("Plaintext is required.");

        // 1) Generate a fresh DEK
        var dek = RandomNumberGenerator.GetBytes(DekSizeBytes);

        try
        {
            // 2) AES-GCM encrypt plaintext with DEK
            var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[TagSizeBytes];
            using (var aesGcm = new AesGcm(dek, TagSizeBytes))
            {
                aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
            }

            // 3) Wrap the DEK using current KEK
            var wrap = await keyProvider.WrapKeyAsync(dek, keyName, ct);
            return new EncryptedPayload
            {
                Ciphertext = ciphertext,
                Nonce = nonce,
                Tag = tag,
                WrapPayload = wrap
            };
        }
        finally
        {
            // Zero out the DEK
            Array.Clear(dek);
        }
    }

    /// <inheritdoc />
    public async Task<string> DecryptAsync(EncryptedPayload payload, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var wrap = payload.WrapPayload;
        byte[]? dek = null;
        try
        {
            // 1) Unwrap DEK using preferred exact KEK version first, then fallback by keyName
            dek = await keyProvider.UnwrapKeyAsync(wrap.WrappedDek, keyName, wrap.KekKeyId, ct);

            // 2) AES-GCM decrypt
            var ptBuf = new byte[payload.Ciphertext.Length];
            using (var aesGcm = new AesGcm(dek, TagSizeBytes))
            {
                aesGcm.Decrypt(payload.Nonce, payload.Ciphertext, payload.Tag, ptBuf);
            }

            return Encoding.UTF8.GetString(ptBuf);
        }
        finally
        {
            if (dek is not null)
            {
                // Zero out the DEK
                Array.Clear(dek);
            }
        }
    }
}
