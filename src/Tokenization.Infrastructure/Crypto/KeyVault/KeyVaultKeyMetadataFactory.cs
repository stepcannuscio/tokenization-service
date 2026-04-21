using Azure.Core;
using Azure.Security.KeyVault.Keys;
using Tokenization.Infrastructure.Crypto.KeyVault.Mapping;

namespace Tokenization.Infrastructure.Crypto.KeyVault;

/// <summary>
/// Factory for creating KeyVaultKeyMetadata instances with proper credential injection.
/// This service handles the creation of cacheable metadata while ensuring credentials are properly injected.
/// </summary>
internal sealed class KeyVaultKeyMetadataFactory
{
    private readonly TokenCredential _credential;

    /// <summary>
    /// Initializes a new instance with the provided credential.
    /// </summary>
    public KeyVaultKeyMetadataFactory(TokenCredential credential)
    {
        _credential = credential;
    }

    /// <summary>
    /// Creates a KeyVaultKeyMetadata from a KeyVaultKey.
    /// </summary>
    public KeyVaultKeyMetadata CreateFromKeyVaultKey(KeyVaultKey key, bool isCurrent = false)
    {
        var versionInfo = key.ToKeyVersionInfo(isCurrent);
        var metadata = new KeyVaultKeyMetadata(key.Id.ToString(), _credential, versionInfo);
        return metadata;
    }

    /// <summary>
    /// Injects the credential into a deserialized metadata object.
    /// </summary>
    public void InjectCredential(KeyVaultKeyMetadata metadata)
    {
        metadata.Credential = _credential;
    }
}
