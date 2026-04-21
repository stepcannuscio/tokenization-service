using Azure.Core;
using Azure.Security.KeyVault.Keys.Cryptography;
using System.Text.Json.Serialization;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Infrastructure.Crypto.KeyVault;

/// <summary>
/// Cacheable metadata for Key Vault keys that can be serialized and used to recreate CryptographyClient instances.
/// This avoids the serialization issues with CryptographyClient while maintaining performance.
/// </summary>
internal sealed class KeyVaultKeyMetadata : IKeyClient<CryptographyClient>
{
    /// <summary>
    /// Initializes a new instance for cache deserialization. Do not use directly in application code.
    /// </summary>
    [JsonConstructor]
    public KeyVaultKeyMetadata() { }

    /// <summary>
    /// Initializes a new instance with the provided metadata.
    /// </summary>
    public KeyVaultKeyMetadata(string keyId, TokenCredential credential, KeyVersionInfo versionInfo)
    {
        KeyId = keyId;
        Credential = credential;
        VersionInfo = versionInfo;
    }

    /// <summary>
    /// The Key Vault key identifier used to create the CryptographyClient.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// The credential used for authentication. This is not serialized and must be provided externally.
    /// </summary>
    [JsonIgnore]
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets the CryptographyClient, creating it lazily when first accessed.
    /// This avoids the serialization issues while maintaining performance.
    /// </summary>
    [JsonIgnore]
    public CryptographyClient Client
    {
        get
        {
            if (_client is null && Credential is not null)
            {
                _client = new CryptographyClient(new Uri(KeyId), Credential);
            }
            return _client ?? throw new InvalidOperationException("Credential must be set before accessing Client");
        }
        set => _client = value;
    }

    private CryptographyClient? _client;

    /// <inheritdoc />
    public KeyVersionInfo VersionInfo { get; set; } = new() { KekKeyId = string.Empty };
}
