using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tokenization.Infrastructure.Caching.Config.Options;

namespace Tokenization.Infrastructure.Caching.Health;

/// <summary>
/// Health check implementation for cache connectivity and performance.
/// This health check verifies that the cache (Redis or in-memory) is accessible and responsive.
/// </summary>
internal sealed class CacheHealthCheck(
    HybridCache hybridCache,
    ILogger<CacheHealthCheck> logger,
    IOptions<CacheOptions> options)
    : IHealthCheck
{
    private readonly CacheOptions _options = options.Value;

    private static HybridCacheEntryOptions HybridCacheOptions() => new()
    {
        Expiration = TimeSpan.FromMinutes(1),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        if (!_options.EnableHealthChecks)
        {
            return HealthCheckResult.Healthy("Cache health checks are disabled");
        }

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var testKey = $"health_check_{Guid.NewGuid():N}";
            const string testValue = "health_check_test_value";

            // Test cache write operation
            await hybridCache.SetAsync(
                testKey,
                testValue,
                HybridCacheOptions(),
                cancellationToken: ct);

            // Test cache read operation
            var retrievedValue = await hybridCache.GetOrCreateAsync<string?>(
                testKey,
                _ => ValueTask.FromResult<string?>(null),
                HybridCacheOptions(),
                cancellationToken: ct);

            // Clean up test data
            await hybridCache.RemoveAsync(testKey, ct);

            stopwatch.Stop();

            if (retrievedValue is not testValue)
            {
                return HealthCheckResult.Unhealthy(
                    "Cache read/write test failed - retrieved value does not match expected value");
            }

            var data = new Dictionary<string, object>
            {
                ["response_time_ms"] = stopwatch.ElapsedMilliseconds,
                ["cache_type"] = string.IsNullOrEmpty(_options.RedisConnectionString) ? "in-memory" : "redis",
                ["instance_name"] = _options.InstanceName ?? "default"
            };

            // Check if response time is acceptable (5 seconds threshold)
            if (stopwatch.ElapsedMilliseconds > _options.HealthCheckTimeoutSeconds * 1000)
            {
                logger.LogWarning("Cache health check took {ElapsedMs}ms, which exceeds threshold of {TimeoutMs}ms",
                    stopwatch.ElapsedMilliseconds, _options.HealthCheckTimeoutSeconds * 1000);

                return HealthCheckResult.Degraded(
                    $"Cache is accessible but slow (took {stopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }

            logger.LogDebug("Cache health check completed successfully in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);

            return HealthCheckResult.Healthy("Cache is accessible and responsive", data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cache health check failed");

            var data = new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["error_type"] = ex.GetType().Name,
                ["cache_type"] = string.IsNullOrEmpty(_options.RedisConnectionString) ? "in-memory" : "redis"
            };

            return HealthCheckResult.Unhealthy(
                $"Cache is not accessible: {ex.Message}",
                ex,
                data);
        }
    }
}