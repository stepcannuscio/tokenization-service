using Microsoft.Extensions.Caching.Hybrid;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Caching;

namespace Tokenization.Infrastructure.Crypto.Caching;

/// <summary>
/// HybridCache-backed cache for key clients and their version metadata.
/// Stores both the list of all known clients for a key and a fast path to the current client.
/// </summary>
/// <typeparam name="TKeyClient">Key client wrapper type holding <see cref="IKeyClient{T}.Client"/> and <see cref="IKeyClient{T}.VersionInfo"/>.</typeparam>
/// <typeparam name="TClient">Underlying concrete client type (e.g., Key Vault client).</typeparam>
/// <remarks>
/// <para>Long expirations are used intentionally; rotation updates are pushed via <see cref="SetClientsAsync"/>.</para>
/// <para>Cache keys are generated using a safe, validated mechanism to prevent collisions and ensure consistency.</para>
/// </remarks>
internal sealed class KeyClientCache<TKeyClient, TClient>(HybridCache cache, ICacheKeyGenerator keyGenerator)
    : IKeyClientCache<TKeyClient, TClient> where TKeyClient : IKeyClient<TClient>
{
    private const string KeyClientNamespace = "KeyClientCache";
    private const string AllClientsSuffix = "all";
    private const string CurrentClientSuffix = "current";

    private static HybridCacheEntryOptions CacheOptions() => new()
    {
        Expiration = TimeSpan.FromDays(365 * 100),
        LocalCacheExpiration = TimeSpan.FromDays(365 * 100)
    };

    /// <inheritdoc />
    public async Task<TKeyClient?> GetClientAsync(string keyName, string keyId, CancellationToken ct = default)
    {
        var clients = await GetAllClientsAsync(keyName, ct);
        return clients.FirstOrDefault(client => client.VersionInfo.KekKeyId == keyId);
    }

    /// <inheritdoc />
    public async Task<TKeyClient?> GetCurrentClientAsync(string keyName, CancellationToken ct = default)
    {
        var cacheKey = GetCurrentClientCacheKey(keyName);
        return await cache.GetOrCreateAsync<TKeyClient>(
            cacheKey,
            _ => default,
            cancellationToken: ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TKeyClient>> GetAllClientsAsync(string keyName,
        CancellationToken ct = default)
    {
        var cacheKey = GetAllClientsCacheKey(keyName);
        return await cache.GetOrCreateAsync<IReadOnlyList<TKeyClient>?>(
            cacheKey,
            _ => default,
            cancellationToken: ct) ?? (IReadOnlyList<TKeyClient>)[];
    }

    /// <inheritdoc />
    public async Task SetClientsAsync(string keyName, IReadOnlyList<TKeyClient> clients, CancellationToken ct = default)
    {
        var ordered = clients
            .OrderByDescending(c => c.VersionInfo.CreatedAt)
            .ToList();

        var allClientsKey = GetAllClientsCacheKey(keyName);
        await SetCache(allClientsKey, (IReadOnlyList<TKeyClient>)ordered, ct);

        var currentClient = clients.FirstOrDefault(client => client.VersionInfo.IsCurrent);
        if (currentClient is not null)
        {
            var currentClientKey = GetCurrentClientCacheKey(keyName);
            await SetCache(currentClientKey, currentClient, ct);
        }
    }

    private async Task SetCache<T>(string cacheKey, T value, CancellationToken token = default)
    {
        await cache.SetAsync(
            cacheKey,
            value,
            options: CacheOptions(),
            cancellationToken: token);
    }

    private string GetAllClientsCacheKey(string keyName)
    {
        return keyGenerator.GenerateKey(KeyClientNamespace, AllClientsSuffix, keyName);
    }

    private string GetCurrentClientCacheKey(string keyName)
    {
        return keyGenerator.GenerateKey(KeyClientNamespace, CurrentClientSuffix, keyName);
    }
}
