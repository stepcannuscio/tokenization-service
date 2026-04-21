using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Tokenization.Infrastructure.Db.Config.Options;

namespace Tokenization.Infrastructure.Db.Health.Config;

/// <summary>
/// DI registration for the database health check.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Adds database health check to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Database options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDbHealthCheck(
        this IServiceCollection services,
        DatabaseOptions options)
    {
        if (!options.EnableHealthChecks)
            return services;

        services.AddHealthChecks()
            .AddCheck<DbHealthCheck>(
                "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "sqlserver", "infrastructure"]);

        return services;
    }
}