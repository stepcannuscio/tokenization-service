using Azure.Security.KeyVault.Keys.Cryptography;
using Tokenization.Domain.ValueObjects;
using Tokenization.Infrastructure.Crypto.Mapping;

namespace Tokenization.Infrastructure.Crypto.KeyVault.Mapping;

/// <summary>
/// Maps a Key Vault <see cref="WrapResult"/> to the domain <see cref="KeyWrapPayload"/>.
/// </summary>
internal sealed class KeyVaultKeyWrapPayloadMapper : IKeyWrapPayloadMapper<WrapResult>
{
    /// <summary>
    /// Converts a Key Vault wrap operation result into a portable payload for storage/transport.
    /// </summary>
    /// <param name="source">The result from <see cref="CryptographyClient.WrapKeyAsync"/>.</param>
    /// <returns>A <see cref="KeyWrapPayload"/> containing wrapped DEK bytes and identifying metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
    public KeyWrapPayload Map(WrapResult source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new KeyWrapPayload
        {
            WrappedDek = source.EncryptedKey,
            Algorithm = source.Algorithm.ToString(),
            KekKeyId = source.KeyId,
            WrappedAt = DateTimeOffset.Now
        };
    }
}
