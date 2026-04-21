using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Config.Options;

namespace Tokenization.Infrastructure.Crypto.KeyVault;

/// <summary>
/// Health check implementation for Azure Key Vault connectivity and key availability.
/// This health check verifies that the Key Vault is accessible and required keys are available.
/// </summary>
internal sealed class KeyVaultHealthCheck(
    KeyClient keyClient,
    IKeyProvider keyProvider,
    ILogger<KeyVaultHealthCheck> logger,
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

            // Test Key Vault connectivity by checking if we can list keys
            var keyProperties = new List<string>();
            await foreach (var property in keyClient.GetPropertiesOfKeysAsync(cancellationToken))
            {
                keyProperties.Add(property.Name);
                if (keyProperties.Count >= 10) // Limit to avoid long operations
                    break;
            }

            stopwatch.Stop();

            // Test key availability for the configured keys
            var keyAvailabilityData = new Dictionary<string, object>();
            var allKeysAvailable = true;

            // Check KEK key availability
            try
            {
                await keyProvider.PreloadKeysAsync(_options.KekKeyName, cancellationToken);
                keyAvailabilityData["kek_key_available"] = true;
            }
            catch (Exception ex)
            {
                keyAvailabilityData["kek_key_available"] = false;
                keyAvailabilityData["kek_key_error"] = ex.Message;
                allKeysAvailable = false;
                logger.LogWarning(ex, "KEK key '{KeyName}' is not available", _options.KekKeyName);
            }

            // Check blind index key availability
            try
            {
                await keyProvider.PreloadKeysAsync(_options.BlindIndexKeyName, cancellationToken);
                keyAvailabilityData["blind_index_key_available"] = true;
            }
            catch (Exception ex)
            {
                keyAvailabilityData["blind_index_key_available"] = false;
                keyAvailabilityData["blind_index_key_error"] = ex.Message;
                allKeysAvailable = false;
                logger.LogWarning(ex, "Blind index key '{KeyName}' is not available", _options.BlindIndexKeyName);
            }

            var data = new Dictionary<string, object>
            {
                ["response_time_ms"] = stopwatch.ElapsedMilliseconds,
                ["vault_url"] = _options.VaultUrl,
                ["kek_key_name"] = _options.KekKeyName,
                ["blind_index_key_name"] = _options.BlindIndexKeyName,
                ["total_keys_found"] = keyProperties.Count
            };

            // Merge key availability data
            foreach (var kvp in keyAvailabilityData)
            {
                data[kvp.Key] = kvp.Value;
            }

            if (!allKeysAvailable)
            {
                return HealthCheckResult.Unhealthy(
                    "Key Vault is accessible but required keys are not available",
                    data: data);
            }

            // Check if response time is acceptable
            if (stopwatch.ElapsedMilliseconds > _options.HealthCheckTimeoutSeconds * 1000)
            {
                logger.LogWarning("Key Vault health check took {ElapsedMs}ms, which exceeds threshold of {TimeoutMs}ms",
                    stopwatch.ElapsedMilliseconds, _options.HealthCheckTimeoutSeconds * 1000);

                return HealthCheckResult.Degraded(
                    $"Key Vault is accessible but slow (took {stopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }

            logger.LogDebug("Key Vault health check completed successfully in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);

            return HealthCheckResult.Healthy("Key Vault is accessible and all required keys are available", data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Key Vault health check failed");

            var data = new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["error_type"] = ex.GetType().Name,
                ["vault_url"] = _options.VaultUrl
            };

            return HealthCheckResult.Unhealthy(
                $"Key Vault is not accessible: {ex.Message}",
                ex,
                data);
        }
    }
}
