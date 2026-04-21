using System.ComponentModel.DataAnnotations;

namespace Tokenization.Api.Health.Config.Options;

/// <summary>
/// Configuration API health checks.
/// </summary>
internal sealed class ApiHealthCheckOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "ApiHealthCheck";

    /// <summary>
    /// Enable health checks for API connectivity.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Health check timeout in seconds.
    /// </summary>
    [Range(0, 10)]
    public int HealthCheckTimeoutSeconds { get; set; } = 1;
}
