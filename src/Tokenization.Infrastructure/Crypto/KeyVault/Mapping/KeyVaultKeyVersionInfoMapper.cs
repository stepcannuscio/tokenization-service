using Azure.Security.KeyVault.Keys;
using Tokenization.Domain.ValueObjects;
using Tokenization.Infrastructure.Crypto.Mapping;

namespace Tokenization.Infrastructure.Crypto.KeyVault.Mapping;

/// <summary>
/// Maps a <see cref="KeyVaultKey"/> to a <see cref="KeyVersionInfo"/> value object.
/// </summary>
internal sealed class KeyVaultKeyVersionInfoMapper : IKeyVersionInfoMapper<KeyVaultKey>
{
    /// <summary>
    /// Projects <see cref="KeyVaultKey.Properties"/> into <see cref="KeyVersionInfo"/>.
    /// </summary>
    /// <param name="source">Source Key Vault key.</param>
    /// <param name="isCurrentKey">Whether the version should be flagged as current.</param>
    /// <returns>A populated <see cref="KeyVersionInfo"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
    public KeyVersionInfo Map(KeyVaultKey source, bool isCurrentKey = false)
    {
        ArgumentNullException.ThrowIfNull(source);
        var props = source.Properties;
        return new KeyVersionInfo
        {
            KekKeyId = props.Id.ToString(),
            CreatedAt = props.CreatedOn ?? DateTimeOffset.MinValue,
            IsCurrent = isCurrentKey
        };
    }
}
