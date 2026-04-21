using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Tokenization.Infrastructure.Caching.Config.Options;

namespace Tokenization.Infrastructure.Caching.Health.Config;

/// <summary>
/// DI registration for the cache health check.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Adds cache health check to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Cache options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCacheHealthCheck(
        this IServiceCollection services,
        CacheOptions options)
    {
        if (!options.EnableHealthChecks)
            return services;

        services.AddHealthChecks()
            .AddCheck<CacheHealthCheck>(
                "cache",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["cache", "redis", "infrastructure"]);

        return services;
    }
}