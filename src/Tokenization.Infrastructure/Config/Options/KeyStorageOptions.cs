using System.ComponentModel.DataAnnotations;
using Tokenization.Infrastructure.Crypto.Enums;

namespace Tokenization.Infrastructure.Config.Options;

/// <summary>
/// Configuration for selecting and parameterizing the key storage provider (in-memory or external vault).
/// </summary>
internal sealed class KeyStorageOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "KeyStorage";

    /// <summary>
    /// The key provider implementation to use for KEK storage and key-wrapping operations.
    /// </summary>
    [Required]
    public required KeyProviderType KeyProvider { get; set; }

    /// <summary>
    /// Base URL for the external key vault (when applicable).
    /// </summary>
    [Required, Url]
    public required string VaultUrl { get; set; }

    /// <summary>
    /// Logical key name used by the provider to resolve current and historical KEK versions.
    /// </summary>
    [Required]
    public required string KekKeyName { get; set; }

    /// <summary>
    /// Logical key name used by the provider to resolve blind index keys for equality searches.
    /// </summary>
    [Required]
    public required string BlindIndexKeyName { get; set; }

    /// <summary>
    /// Use in-memory key storage (primarily for local/dev scenarios); bypasses external vault integration.
    /// </summary>
    public bool UseInMemoryKeys { get; set; } = false;

    /// <summary>
    /// Enable health checks for key provider connectivity and functionality.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Health check timeout in seconds.
    /// </summary>
    [Range(1, 60)]
    public int HealthCheckTimeoutSeconds { get; set; } = 10;
}