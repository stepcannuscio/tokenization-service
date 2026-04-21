using System.ComponentModel.DataAnnotations;

namespace Tokenization.Infrastructure.Caching.Config.Options;

/// <summary>
/// Configuration for the hybrid cache used by crypto/key infrastructure (local + optional Redis).
/// </summary>
internal sealed class CacheOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Cache";

    /// <summary>
    /// Optional Redis connection string. If omitted, the <c>HybridCache</c> operates in memory-only mode.
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Optional logical instance/prefix to namespace cache keys for multi-tenant or multi-environment deployments.
    /// </summary>
    [RegularExpression("^[a-zA-Z0-9:_-]*$")]
    public string? InstanceName { get; set; }

    /// <summary>
    /// Enable health checks for cache connectivity.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Health check timeout in seconds.
    /// </summary>
    [Range(1, 30)]
    public int HealthCheckTimeoutSeconds { get; set; } = 5;
}
