using System.Security.Cryptography;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Infrastructure.Crypto.InMemory;

/// <summary>
/// Development-only <see cref="IKeyProvider"/> that manages KEKs in memory and performs local AES-based wrap/unwrap.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description>Wrap uses AES-CBC for transport in dev; production scenarios should use a KMS (e.g., Key Vault) with A256KW.</description></item>
///   <item><description>Clients are retrieved from <see cref="IKeyClientCache{TKeyClient, TClient}"/> and rotated in process.</description></item>
///   <item><description>No external persistence is performed; keys are lost on process restart unless cached externally.</description></item>
/// </list>
/// </remarks>
internal sealed class InMemoryKeyProvider(IKeyClientCache<InMemoryKeyClient, byte[]> cache) : IKeyProvider
{
    /// <inheritdoc />
    public async Task<KeyWrapPayload> WrapKeyAsync(byte[] dek, string keyName, CancellationToken ct = default)
    {
        var client = await cache.GetCurrentClientAsync(keyName, ct);
        if (client is not null) return await WrapWithClientAsync(client, dek);

        var clients = await cache.GetAllClientsAsync(keyName, ct);
        client = clients.MaxBy(h => h.VersionInfo.CreatedAt);
        if (client is null)
        {
            throw new InvalidOperationException($"No KEKs available to wrap DEK with for '{keyName}'");
        }

        return await WrapWithClientAsync(client, dek);
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
            var client = await cache.GetClientAsync(keyName, keyId, ct);
            if (client is not null)
            {
                try
                {
                    return await UnwrapWithClientAsync(client, wrappedDek);
                }
                catch (CryptographicException)
                {
                    // fall through to other versions
                }
            }
        }

        // 2) Fallback: try all known versions for keyName, newest first
        var clients = await cache.GetAllClientsAsync(keyName, ct);
        foreach (var client in clients)
        {
            try
            {
                return await UnwrapWithClientAsync(client, wrappedDek);
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
        var clients = await cache.GetAllClientsAsync(keyName, ct);
        var currentClients = clients.Select(c =>
        {
            if (c.VersionInfo.IsCurrent)
            {
                c.VersionInfo = c.VersionInfo with { IsCurrent = false };
            }

            return c;
        }).ToList();

        var newClient = new InMemoryKeyClient(keyName, currentClients.Count + 1, true);
        currentClients.Add(newClient);
        await cache.SetClientsAsync(keyName, currentClients, ct);
    }

    /// <inheritdoc />
    public async Task ReloadKeysAsync(string keyName, CancellationToken ct = default)
    {
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task PreloadKeysAsync(string keyName, CancellationToken ct = default)
    {
        var existingClients = await cache.GetAllClientsAsync(keyName, ct);
        if (existingClients.Count > 0)
        {
            return;
        }

        var newClient = new InMemoryKeyClient(keyName, 1, true);
        await cache.SetClientsAsync(keyName, [newClient], ct);
    }

    /// <inheritdoc />
    public async Task<byte[]> SignDataAsync(byte[] data, string keyName, string? keyId, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(keyId))
        {
            var exactClient = await cache.GetClientAsync(keyName, keyId, ct);
            if (exactClient is not null) return SignWithClientAsync(exactClient.Client, data);
        }

        var client = await GetCurrentClientAsync(keyName, ct);
        if (client is not null) return SignWithClientAsync(client, data);

        throw new InvalidOperationException($"Unable to sign data using any key for '{keyName}'.");
    }

    private async Task<byte[]?> GetCurrentClientAsync(
        string keyName,
        CancellationToken ct = default)
    {
        var client = await cache.GetCurrentClientAsync(keyName, ct);
        if (client is not null) return client.Client;

        var clients = await cache.GetAllClientsAsync(keyName, ct);
        var mostRecentClient = clients.MaxBy(h => h.VersionInfo.CreatedAt);
        return mostRecentClient?.Client;
    }

    private static async Task<KeyWrapPayload> WrapWithClientAsync(InMemoryKeyClient client, byte[] dek)
    {
        using var aes = Aes.Create();
        aes.Key = client.Client;
        aes.GenerateIV();
        using var enc = aes.CreateEncryptor();
        var cipher = enc.TransformFinalBlock(dek, 0, dek.Length);
        var wrapped = aes.IV.Concat(cipher).ToArray();

        return await Task.FromResult(new KeyWrapPayload
        {
            WrappedDek = wrapped,
            KekKeyId = client.VersionInfo.KekKeyId,
            Algorithm = "AES-CBC-DEV", // Dev-only transport; production uses A256KW in KMS
            WrappedAt = DateTimeOffset.UtcNow
        });
    }

    private static async Task<byte[]> UnwrapWithClientAsync(InMemoryKeyClient client, byte[] wrappedDek)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = client.Client;
            var iv = wrappedDek.AsSpan(0, 16).ToArray();
            var cipher = wrappedDek.AsSpan(16).ToArray();
            aes.IV = iv;
            using var dec = aes.CreateDecryptor();
            return await Task.FromResult(dec.TransformFinalBlock(cipher, 0, cipher.Length));
        }
        catch
        {
            throw new CryptographicException("Failed to unwrap dek.");
        }
    }

    private static byte[] SignWithClientAsync(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }
}
