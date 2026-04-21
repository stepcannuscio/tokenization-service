namespace Tokenization.Api.Idempotency;

/// <summary>
/// Defines a contract for generating cache keys for idempotency operations.
/// </summary>
internal interface IIdempotencyKeyHasher
{
    /// <summary>
    /// Generates a unique cache key for an idempotent request.
    /// Note: For PCI compliance, this method does NOT include request body content in the hash
    /// to avoid processing sensitive card data. The client is responsible for ensuring
    /// different requests use different idempotency keys.
    /// </summary>
    /// <param name="partition">The partition identifier (user ID or IP address) for cache isolation.</param>
    /// <param name="method">The HTTP method of the request.</param>
    /// <param name="path">The request path.</param>
    /// <param name="idempotencyKey">The client-provided idempotency key.</param>
    /// <returns>A unique cache key for the idempotent request.</returns>
    string Hash(string partition, string method, PathString path, string idempotencyKey);
}