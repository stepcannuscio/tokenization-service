using Azure.Core;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Infrastructure.Crypto.KeyVault.Mapping;

/// <summary>
/// Convenience extension methods for mapping Key Vault SDK types to domain models.
/// </summary>
internal static class KeyVaultMappingExtensions
{
    /// <summary>
    /// Maps a <see cref="KeyVaultKey"/> to <see cref="KeyVersionInfo"/>.
    /// </summary>
    public static KeyVersionInfo ToKeyVersionInfo(this KeyVaultKey key, bool isCurrentKey = false) =>
        new KeyVaultKeyVersionInfoMapper().Map(key, isCurrentKey);

    /// <summary>
    /// Maps a Key Vault <see cref="WrapResult"/> to a <see cref="KeyWrapPayload"/>.
    /// </summary>
    public static KeyWrapPayload ToKeyWrapPayload(this WrapResult wrapResult) =>
        new KeyVaultKeyWrapPayloadMapper().Map(wrapResult);
}
