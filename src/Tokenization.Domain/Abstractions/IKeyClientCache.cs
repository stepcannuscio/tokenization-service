namespace Tokenization.Domain.Abstractions;

/// <summary>
/// Defines the contract for a cache that stores and manages cryptographic key clients.
/// </summary>
/// <remarks>
/// This cache is used by the tokenization domain to avoid unnecessary round-trips
/// to external key providers (e.g., Azure Key Vault) when resolving key material.
/// </remarks>
internal interface IKeyClientCache<TKeyClient, TClient> where TKeyClient : IKeyClient<TClient>
{
    /// <summary>
    /// Retrieves a specific key client from the cache, if available.
    /// </summary>
    /// <param name="keyName">Logical KEK name (e.g., "payment-kek") used to select the key client.</param>
    /// <param name="keyId">Fully-qualified KEK identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    /// The <see cref="IKeyClient{T}"/> for the requested version, or <c>null</c>
    /// if the key client is not present in the cache.
    /// </returns>
    Task<TKeyClient?> GetClientAsync(string keyName, string keyId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the current key client from the cache, if available.
    /// </summary>
    /// <param name="keyName">Logical KEK name (e.g., "payment-kek") used to select the key client.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    /// The <see cref="IKeyClient{T}"/> for the requested version, or <c>null</c>
    /// if the key client is not present in the cache.
    /// </returns>
    Task<TKeyClient?> GetCurrentClientAsync(string keyName, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all known key clients currently stored in the cache.
    /// </summary>
    /// <param name="keyName">Logical KEK name (e.g., "payment-kek") used to select the key client.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    /// A read-only list of <see cref="IKeyClient{T}"/> objects, representing all cached active key clients.
    /// </returns>
    Task<IReadOnlyList<TKeyClient>> GetAllClientsAsync(string keyName, CancellationToken ct = default);

    /// <summary>
    /// Sets key clients in the cache.
    /// </summary>
    /// <param name="keyName">Logical KEK name (e.g., "payment-kek") used to select the key client.</param>
    /// <param name="clients">A read-only list of <see cref="IKeyClient{T}"/> objects, representing all cached key clients.</param>
    /// <param name="ct">A cancellation token.</param>
    Task SetClientsAsync(string keyName, IReadOnlyList<TKeyClient> clients, CancellationToken ct = default);
}