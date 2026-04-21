using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Tokenization.Api.Idempotency.Config.Options;

namespace Tokenization.Api.Idempotency;

/// <summary>
/// Middleware that provides idempotency support for data-modifying HTTP requests using HybridCache
/// to cache responses and replay them when the same idempotency key is used within the TTL window.
/// <para>
/// This middleware ensures that duplicate requests with the same idempotency key return
/// the same response without re-executing the underlying operation, providing protection
/// against network retries and client-side duplicate submissions.
/// </para>
/// <para>
/// PCI COMPLIANCE NOTE: This implementation is designed for PCI-compliant environments:
/// - Does NOT read or hash request body content to avoid processing sensitive card data
/// - Relies on client-provided idempotency keys for request uniqueness
/// - Clients must ensure different requests use different idempotency keys
/// </para>
/// <para>
/// Key features:
/// - Uses HybridCache for optimal performance (in-memory + distributed caching)
/// - Applies to POST, PUT, and PATCH requests (data-modifying operations)
/// - Requires <see cref="IdempotencyHeaders.IdempotencyKey"/> header for data-modifying requests
/// - Partitions cache by user identity or IP address for security
/// - Caches only successful responses (2xx status codes)
/// - Includes request method and path in cache key for specificity
/// - Adds <see cref="IdempotencyHeaders.IdempotencyReplay"/> header to indicate cached responses
/// - PCI-compliant: No sensitive data processing in cache key generation
/// - Atomic cache operations using GetOrCreateAsync for thread safety
/// </para>
/// </summary>
internal sealed class IdempotencyMiddleware(
    RequestDelegate next,
    HybridCache cache,
    IOptions<IdempotencyOptions> options,
    IIdempotencyKeyHasher hasher)
{
    private readonly IdempotencyOptions _opts = options.Value;

    /// <summary>
    /// Processes the HTTP request to provide idempotency functionality.
    /// </summary>
    /// <param name="context">The HTTP context containing request and response information.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsIdempotencyRequired(context))
        {
            var key = GetIdempotencyKey(context);
            if (string.IsNullOrEmpty(key))
            {
                await HandleMissingIdempotencyKey(context);
                return;
            }
            
            var partition = GetPartitionKey(context);
            var cacheKey = hasher.Hash(partition, context.Request.Method, context.Request.Path, key);

            var cacheOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(_opts.TtlSeconds)
            };

            var cachedResponse = await cache.GetOrCreateAsync<CachedResponse?>(
                cacheKey,
                async ct => await CacheResponse(context, ct),
                cacheOptions);

            if (cachedResponse is not null && !context.Response.HasStarted)
            {
                await ReplayCachedResponse(context, cachedResponse);
            }
            return;
        }

        await next(context);
    }

    private static bool IsIdempotencyRequired(HttpContext context)
    {
        // Only apply idempotency to data-modifying HTTP methods
        return IsDataModifying(context.Request.Method);
    }

    public static bool IsDataModifying(string method)
    {
        return HttpMethods.IsPost(method) ||
               HttpMethods.IsPut(method) ||
               HttpMethods.IsPatch(method);
    }
    
    private static string? GetIdempotencyKey(HttpContext context)
    {
        return context.Request.Headers.TryGetValue(IdempotencyHeaders.IdempotencyKey, out var key)
            ? key.ToString()
            : null;
    }

    private static async Task HandleMissingIdempotencyKey(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Missing Idempotency-Key header",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Idempotency-Key header is required for data-modifying operations (POST, PUT, PATCH)."
        });
    }
    
    private static string GetPartitionKey(HttpContext context)
    {
        // Create partition key for cache isolation - use authenticated user ID or IP address.
        return context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anon"
            : context.Connection.RemoteIpAddress?.ToString() ?? "anon";
    }

    private async Task<CachedResponse?> CacheResponse(HttpContext context, CancellationToken ct)
    {
        // Capture response
        var originalBody = context.Response.Body;
        await using var mem = new MemoryStream();
        context.Response.Body = mem;

        try
        {
            await next(context);

            // Store only for 2xx responses
            if (context.Response.StatusCode is >= 200 and < 300)
            {
                mem.Position = 0;
                var body = await new StreamReader(mem).ReadToEndAsync(ct);
                var headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

                mem.Position = 0;
                await mem.CopyToAsync(originalBody, context.RequestAborted);
                context.Response.Body = originalBody;

                return new CachedResponse
                {
                    StatusCode = context.Response.StatusCode,
                    Body = body,
                    Headers = headers
                };
            }

            // Don't cache error responses
            mem.Position = 0;
            await mem.CopyToAsync(originalBody, context.RequestAborted);
            context.Response.Body = originalBody;
            return null;
        }
        catch
        {
            // Ensure original body is restored before exception propagates
            context.Response.Body = originalBody;
            throw;
        }
    }

    private static async Task ReplayCachedResponse(HttpContext context, CachedResponse cachedResponse)
    {
        context.Response.StatusCode = cachedResponse.StatusCode;
        foreach (var kv in cachedResponse.Headers)
        {
            context.Response.Headers[kv.Key] = kv.Value;
        }
            
        context.Response.Headers[IdempotencyHeaders.IdempotencyReplay] = "true";
        await context.Response.WriteAsync(cachedResponse.Body);
    }

    /// <summary>
    /// Represents a cached HTTP response that can be replayed for idempotent requests.
    /// </summary>
    private sealed record CachedResponse
    {
        public int StatusCode { get; init; }
        public string Body { get; init; } = string.Empty;
        public Dictionary<string, string> Headers { get; init; } = new();
    }
}