namespace Tokenization.Domain.ValueObjects;

/// <summary>
/// Authenticated encryption payload for a single record, including the wrapped DEK and the KEK identifier
/// that wrapped it. Persist this with the token record.
/// </summary>
internal sealed record EncryptedPayload
{
    /// <summary>
    /// Encrypted sensitive payment data.
    /// Could include PAN, expiry, cardholder name for cards, or cryptographic payment tokens for APMs.
    /// Must never leave the PCI-scoped environment unencrypted.
    /// </summary>
    public required byte[] Ciphertext { get; init; }

    /// <summary>
    /// AES-GCM nonce/IV (recommended 12 bytes).
    /// </summary>
    public required byte[] Nonce { get; init; }

    /// <summary>
    /// AES-GCM authentication tag (e.g., 16 bytes for 128-bit tag).
    /// </summary>
    public required byte[] Tag { get; init; }
    
    /// <summary>
    /// Result of wrapping a freshly generated DEK with a KEK.
    /// </summary>
    public required KeyWrapPayload WrapPayload { get; init; }
}