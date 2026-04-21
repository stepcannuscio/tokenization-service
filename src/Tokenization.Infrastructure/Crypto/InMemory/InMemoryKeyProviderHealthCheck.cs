using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Config.Options;

namespace Tokenization.Infrastructure.Crypto.InMemory;

/// <summary>
/// Health check implementation for in-memory key provider functionality.
/// This health check verifies that the in-memory key provider can perform key operations.
/// </summary>
internal sealed class InMemoryKeyProviderHealthCheck(
    IKeyProvider keyProvider,
    ILogger<InMemoryKeyProviderHealthCheck> logger,
    IOptions<KeyStorageOptions> options)
    : IHealthCheck
{
    private readonly KeyStorageOptions _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableHealthChecks)
        {
            return HealthCheckResult.Healthy("Key storage health checks are disabled");
        }

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Test key provider functionality with a simple key operation
            var testKeyName = $"health_check_test_{Guid.NewGuid():N}";
            var testData = new byte[] { 1, 2, 3, 4, 5 };

            // Test key preloading
            await keyProvider.PreloadKeysAsync(testKeyName, cancellationToken);

            // Test key wrapping
            var wrappedKey = await keyProvider.WrapKeyAsync(testData, testKeyName, cancellationToken);

            // Test key unwrapping
            var unwrappedKey = await keyProvider.UnwrapKeyAsync(
                wrappedKey.WrappedDek,
                testKeyName,
                wrappedKey.KekKeyId,
                cancellationToken);

            // Test data signing
            var signature =
                await keyProvider.SignDataAsync(testData, testKeyName, wrappedKey.KekKeyId, cancellationToken);

            stopwatch.Stop();

            // Verify that unwrapped key matches original data
            if (!testData.SequenceEqual(unwrappedKey))
            {
                return HealthCheckResult.Unhealthy(
                    "Key wrap/unwrap test failed - unwrapped data does not match original");
            }

            // Verify signature is not empty
            if (signature.Length == 0)
            {
                return HealthCheckResult.Unhealthy("Data signing test failed - signature is empty");
            }

            var data = new Dictionary<string, object>
            {
                ["response_time_ms"] = stopwatch.ElapsedMilliseconds,
                ["key_provider_type"] = "in-memory",
                ["kek_key_name"] = _options.KekKeyName,
                ["blind_index_key_name"] = _options.BlindIndexKeyName,
                ["test_key_operations"] = "wrap,unwrap,sign"
            };

            // Check if response time is acceptable
            if (stopwatch.ElapsedMilliseconds > _options.HealthCheckTimeoutSeconds * 1000)
            {
                logger.LogWarning(
                    "In-memory key provider health check took {ElapsedMs}ms, which exceeds threshold of {TimeoutMs}ms",
                    stopwatch.ElapsedMilliseconds, _options.HealthCheckTimeoutSeconds * 1000);

                return HealthCheckResult.Degraded(
                    $"In-memory key provider is functional but slow (took {stopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }

            logger.LogDebug("In-memory key provider health check completed successfully in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);

            return HealthCheckResult.Healthy(
                "In-memory key provider is functional and performing key operations correctly", data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "In-memory key provider health check failed");

            var data = new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["error_type"] = ex.GetType().Name,
                ["key_provider_type"] = "in-memory"
            };

            return HealthCheckResult.Unhealthy(
                $"In-memory key provider is not functional: {ex.Message}",
                ex,
                data);
        }
    }
}