namespace Tokenization.Infrastructure.Caching;

/// <summary>
/// Provides safe generation of cache keys with proper namespacing, validation, and collision prevention.
/// </summary>
internal interface ICacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key with proper namespacing and validation.
    /// </summary>
    /// <param name="namespace">The namespace for the cache key (e.g., "KeyClientCache", "TokenCache").</param>
    /// <param name="keyParts">The key parts to combine into a cache key.</param>
    /// <returns>A validated cache key string.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    string GenerateKey(string @namespace, params string[] keyParts);

    /// <summary>
    /// Generates a cache key with version information for versioned resources.
    /// </summary>
    /// <param name="namespace">The namespace for the cache key.</param>
    /// <param name="version">The version identifier.</param>
    /// <param name="keyParts">The key parts to combine into a cache key.</param>
    /// <returns>A validated cache key string with version information.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    string GenerateVersionedKey(string @namespace, string version, params string[] keyParts);

    /// <summary>
    /// Validates that a cache key meets the requirements for safe usage.
    /// </summary>
    /// <param name="cacheKey">The cache key to validate.</param>
    /// <returns>True if the cache key is valid, false otherwise.</returns>
    bool IsValidCacheKey(string cacheKey);

    /// <summary>
    /// Sanitizes input to ensure it's safe for use in cache keys.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <returns>A sanitized string safe for cache key usage.</returns>
    string SanitizeForCacheKey(string input);
}