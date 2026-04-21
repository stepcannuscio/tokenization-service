using System.Security.Cryptography;
using System.Text;

namespace Tokenization.Api.Idempotency;

/// <summary>
/// Default implementation of IIdempotencyKeyHasher that creates SHA256-based cache keys.
/// </summary>
internal sealed class DefaultIdempotencyKeyHasher : IIdempotencyKeyHasher
{
    /// <summary>
    /// Generates a unique cache key by hashing the partition, method, path, and idempotency key.
    /// Note: For PCI compliance, this method does NOT include request body content in the hash
    /// to avoid processing sensitive card data.
    /// </summary>
    /// <param name="partition">The partition identifier (user ID or IP address) for cache isolation.</param>
    /// <param name="method">The HTTP method of the request.</param>
    /// <param name="path">The request path.</param>
    /// <param name="idempotencyKey">The client-provided idempotency key.</param>
    /// <returns>A unique cache key prefixed with "idem:" and followed by the SHA256 hash.</returns>
    public string Hash(string partition, string method, PathString path, string idempotencyKey)
    {
        var input = Encoding.UTF8.GetBytes($"{partition}\n{method}\n{path}\n{idempotencyKey}");
        var hash = SHA256.HashData(input);
        return $"idem:{Convert.ToHexString(hash)}";
    }
}
