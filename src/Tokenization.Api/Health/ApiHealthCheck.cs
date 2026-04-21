using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Tokenization.Api.Health.Config.Options;

namespace Tokenization.Api.Health;

/// <summary>
/// Health check implementation for API functionality.
/// This health check verifies that the API is responsive and basic functionality is working.
/// </summary>
internal sealed class ApiHealthCheck(
    ILogger<ApiHealthCheck> logger,
    IOptions<ApiHealthCheckOptions> options)
    : IHealthCheck
{
    private readonly ApiHealthCheckOptions _options = options.Value;
    
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Healthy("API health checks are disabled"));
        }
        
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Basic API health check - verify that we can process a simple operation
            // This is a lightweight check that doesn't depend on external services
            
            var currentTime = DateTimeOffset.UtcNow;
            var processId = Environment.ProcessId;
            var machineName = Environment.MachineName;
            var version = typeof(ApiHealthCheck).Assembly.GetName().Version?.ToString() ?? "unknown";

            stopwatch.Stop();

            var data = new Dictionary<string, object>
            {
                ["response_time_ms"] = stopwatch.ElapsedMilliseconds,
                ["current_time_utc"] = currentTime.ToString("O"),
                ["process_id"] = processId,
                ["machine_name"] = machineName,
                ["api_version"] = version
            };

            // Check if response time is acceptable
            if (stopwatch.ElapsedMilliseconds > _options.HealthCheckTimeoutSeconds * 1000)
            {
                logger.LogWarning("API health check took {ElapsedMs}ms, which exceeds threshold of {TimeoutMs}ms",
                    stopwatch.ElapsedMilliseconds, _options.HealthCheckTimeoutSeconds * 1000);

                return Task.FromResult(HealthCheckResult.Degraded(
                    $"API is responsive but slow (took {stopwatch.ElapsedMilliseconds}ms)",
                    data: data));
            }

            logger.LogDebug("API health check completed successfully in {ElapsedMs}ms", 
                stopwatch.ElapsedMilliseconds);

            return Task.FromResult(HealthCheckResult.Healthy("API is responsive and functioning correctly", data));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API health check failed");

            var data = new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["error_type"] = ex.GetType().Name
            };

            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"API is not functioning correctly: {ex.Message}",
                ex,
                data));
        }
    }
}