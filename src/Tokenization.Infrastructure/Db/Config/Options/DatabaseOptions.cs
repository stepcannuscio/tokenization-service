using System.ComponentModel.DataAnnotations;

using Tokenization.Infrastructure.Db.Enums;

namespace Tokenization.Infrastructure.Db.Config.Options;

/// <summary>
/// Configuration for the database with comprehensive resilience settings.
/// </summary>
internal sealed class DatabaseOptions
{
    /// <summary>
    /// Configuration section name: <c>Database</c>.
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Database connection string.
    /// </summary>
    [Required]
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Database provider used by EF Core.
    /// </summary>
    public DatabaseProviderType Provider { get; set; } = DatabaseProviderType.SqlServer;

    /// <summary>
    /// Maximum retry count for transient failures.
    /// </summary>
    [Range(1, 10)]
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Maximum delay between retries in seconds.
    /// </summary>
    [Range(1, 60)]
    public int MaxRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Base delay for exponential backoff in seconds.
    /// </summary>
    [Range(1, 10)]
    public int BaseDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Command timeout in seconds.
    /// </summary>
    [Range(10, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    [Range(5, 60)]
    public int ConnectionTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Maximum connection pool size.
    /// </summary>
    [Range(1, 200)]
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Minimum connection pool size.
    /// </summary>
    [Range(0, 50)]
    public int MinPoolSize { get; set; } = 5;

    /// <summary>
    /// Connection lifetime in seconds before it's recreated.
    /// </summary>
    [Range(60, 3600)]
    public int ConnectionLifetimeSeconds { get; set; } = 300;

    /// <summary>
    /// Enable health checks for database connectivity.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Health check timeout in seconds.
    /// </summary>
    [Range(1, 30)]
    public int HealthCheckTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Enable query performance logging for slow queries.
    /// </summary>
    public bool EnablePerformanceLogging { get; set; } = true;

    /// <summary>
    /// Slow query threshold in milliseconds.
    /// </summary>
    [Range(100, 5000)]
    public int SlowQueryThresholdMs { get; set; } = 1000;
    
    /// <summary>
    /// Optional override for SQL Server certificate trust behavior.
    /// When unset, any value already present in the connection string is preserved.
    /// Should only be used for development when set to <c>true</c>.
    /// </summary>
    public bool? TrustServerCertificate { get; set; }
}
