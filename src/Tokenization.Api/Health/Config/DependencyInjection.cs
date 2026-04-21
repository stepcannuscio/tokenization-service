using Microsoft.Extensions.Diagnostics.HealthChecks;
using Tokenization.Api.Health.Config.Options;

namespace Tokenization.Api.Health.Config;

/// <summary>
/// DI registration for the API health checks.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Adds API health check to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The configuration root.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiHealthCheck(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(ApiHealthCheckOptions.SectionName).Get<ApiHealthCheckOptions>();
        if (options?.Enabled ?? false)
        {
            services.AddHealthChecks()
                .AddCheck<ApiHealthCheck>(
                    "api",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["api", "application"]);
        }

        return services;
    }
}
