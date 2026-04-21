using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tokenization.Infrastructure.Db.Config.Options;

namespace Tokenization.Infrastructure.Db.Health;

/// <summary>
/// Health check implementation for database connectivity and performance.
/// This health check verifies that the database is accessible and responsive.
/// </summary>
internal sealed class DbHealthCheck(
    TokensDbContext dbContext,
    ILogger<DbHealthCheck> logger,
    IOptions<DatabaseOptions> options)
    : IHealthCheck
{
    private readonly DatabaseOptions _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableHealthChecks)
        {
            return HealthCheckResult.Healthy("Database health checks are disabled");
        }

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Test basic connectivity by checking if we can access the database
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            
            stopwatch.Stop();

            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Database is not accessible");
            }

            var data = new Dictionary<string, object>
            {
                ["response_time_ms"] = stopwatch.ElapsedMilliseconds,
                ["database_name"] = nameof(TokensDbContext)
            };

            // Check if response time is acceptable
            if (stopwatch.ElapsedMilliseconds > _options.HealthCheckTimeoutSeconds * 1000)
            {
                logger.LogWarning("Database health check took {ElapsedMs}ms, which exceeds timeout of {TimeoutMs}ms",
                    stopwatch.ElapsedMilliseconds, _options.HealthCheckTimeoutSeconds * 1000);

                return HealthCheckResult.Degraded(
                    $"Database is accessible but slow (took {stopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }

            logger.LogDebug("Database health check completed successfully in {ElapsedMs}ms", 
                stopwatch.ElapsedMilliseconds);

            return HealthCheckResult.Healthy("Database is accessible and responsive", data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database health check failed");

            var data = new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["error_type"] = ex.GetType().Name
            };

            return HealthCheckResult.Unhealthy(
                $"Database is not accessible: {ex.Message}",
                ex,
                data);
        }
    }
}