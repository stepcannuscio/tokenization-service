using Azure;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using System.Security.Cryptography;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.ValueObjects;
using Tokenization.Infrastructure.Crypto.KeyVault.Mapping;

namespace Tokenization.Infrastructure.Crypto.KeyVault;

/// <summary>
/// Azure Key Vault-backed KEK provider.
/// Performs server-side wrap/unwrap with <see cref="CryptographyClient"/> and caches per-version metadata.
/// Raw KEK material never leaves Key Vault.
/// </summary>
internal sealed class KeyVaultProvider(
    KeyClient keyClient,
    IKeyClientCache<KeyVaultKeyMetadata, CryptographyClient> cache,
    KeyVaultKeyMetadataFactory metadataFactory) : IKeyProvider
{
    /// <inheritdoc />
    /// <exception cref="RequestFailedException">Thrown when the request to wrap the key fails.</exception>
    /// <exception cref="CryptographicException">Thrown when the client fails to wrap the key.</exception>
    public async Task<KeyWrapPayload> WrapKeyAsync(byte[] dek, string keyName, CancellationToken ct = default)
    {
        var client = await GetCurrentClientWithCredentialAsync(keyName, ct);
        if (client is null)
        {
            throw new InvalidOperationException($"No KEKs available to wrap DEK with for '{keyName}'");
        }
        
        return await WrapWithClientAsync(client.Client, dek, ct);
    }
    
    /// <inheritdoc />
    public async Task<byte[]> UnwrapKeyAsync(
        byte[] wrappedDek,
        string keyName,
        string? keyId,
        CancellationToken ct = default)
    {
        // 1) Fast path: try the exact version if provided and cached
        if (!string.IsNullOrWhiteSpace(keyId))
        {
            var client = await GetClientWithCredentialAsync(keyName, keyId, ct);
            if (client is not null)
            {
                try
                {
                    return await UnwrapWithClientAsync(client.Client, wrappedDek, ct);
                }
                catch (CryptographicException)
                {
                    // fall through to other versions
                }
            }
        }

        // 2) Fallback: try all known versions for keyName, newest first
        var clients = await GetAllClientsWithCredentialAsync(keyName, ct);
        foreach (var client in clients)
        {
            try
            {
                return await UnwrapWithClientAsync(client.Client, wrappedDek, ct);
            }
            catch (CryptographicException)
            {
                /* try next version */
            }
        }

        throw new InvalidOperationException($"Unable to unwrap DEK using any KEK for '{keyName}'.");
    }

    /// <inheritdoc />
    public async Task RotateKeyAsync(string keyName, CancellationToken ct = default)
    {
        // Create a new RSA key version for standard Azure Key Vault
        var opts = new CreateRsaKeyOptions(name: keyName, hardwareProtected: false) { KeySize = 2048 };
        await keyClient.CreateRsaKeyAsync(opts, ct);
        await ReloadKeysAsync(keyName, ct);
    }

    /// <inheritdoc />
    public async Task ReloadKeysAsync(string keyName, CancellationToken ct = default)
    {
        var currentKey = await keyClient.GetKeyAsync(keyName, null, ct);
        var currentKeyId = currentKey.HasValue ? currentKey.Value.Id : null;
        var clients = new List<KeyVaultKeyMetadata>();
        await foreach (var property in keyClient.GetPropertiesOfKeyVersionsAsync(keyName, ct))
        {
            if (property.Enabled is not true || property.ExpiresOn < DateTimeOffset.UtcNow) continue;
            var key = await keyClient.GetKeyAsync(keyName, property.Version, ct);
            if (!key.HasValue) continue;
            clients.Add(metadataFactory.CreateFromKeyVaultKey(key.Value, key.Value.Id == currentKeyId));
        }

        clients = clients.OrderByDescending(version => version.VersionInfo.CreatedAt).ToList();
        await cache.SetClientsAsync(keyName, clients, ct);
    }

    /// <inheritdoc />
    public async Task PreloadKeysAsync(string keyName, CancellationToken ct = default)
    {
        await ReloadKeysAsync(keyName, ct);
    }
    
    /// <inheritdoc />
    /// <exception cref="RequestFailedException">Thrown when request to sign the data fails.</exception>
    public async Task<byte[]> SignDataAsync(byte[] data, string keyName, string? keyId, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(keyId))
        {
            var exactClient = await GetClientWithCredentialAsync(keyName, keyId, ct);
            if (exactClient is not null) return await SignWithClientAsync(exactClient.Client, data, ct);
        }
        
        var client = await GetCurrentClientWithCredentialAsync(keyName, ct);
        if (client is not null) return await SignWithClientAsync(client.Client, data, ct);

        throw new InvalidOperationException($"Unable to sign data using any key for '{keyName}'.");
    }
    
    private async Task<KeyVaultKeyMetadata?> GetCurrentClientWithCredentialAsync(
        string keyName,
        CancellationToken ct = default)
    {
        var client = await cache.GetCurrentClientAsync(keyName, ct);
        if (client is not null)
        {
            metadataFactory.InjectCredential(client);
            return client;
        }

        var clients = await GetAllClientsWithCredentialAsync(keyName, ct);
        return clients.MaxBy(h => h.VersionInfo.CreatedAt);
    }

    private async Task<KeyVaultKeyMetadata?> GetClientWithCredentialAsync(
        string keyName,
        string keyId,
        CancellationToken ct = default)
    {
        var client = await cache.GetClientAsync(keyName, keyId, ct);
        if (client is not null)
        {
            metadataFactory.InjectCredential(client);
        }
        return client;
    }

    private async Task<IReadOnlyList<KeyVaultKeyMetadata>> GetAllClientsWithCredentialAsync(
        string keyName,
        CancellationToken ct = default)
    {
        var clients = await cache.GetAllClientsAsync(keyName, ct);
        foreach (var client in clients)
        {
            metadataFactory.InjectCredential(client);
        }
        return clients;
    }

    private static async Task<KeyWrapPayload> WrapWithClientAsync(
        CryptographyClient client,
        byte[] dek,
        CancellationToken ct = default)
    {
        var wrapResult = await client.WrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, dek, ct);
        return wrapResult.ToKeyWrapPayload();
    }

    private static async Task<byte[]> UnwrapWithClientAsync(
        CryptographyClient client,
        byte[] wrappedDek,
        CancellationToken ct = default)
    {
        var res = await client.UnwrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, wrappedDek, ct);
        return res.Key;
    }
    
    private static async Task<byte[]> SignWithClientAsync(
        CryptographyClient client,
        byte[] data,
        CancellationToken ct = default)
    {
        var sign = await client.SignDataAsync(SignatureAlgorithm.RS256, data, ct);
        return sign.Signature;
    }
}