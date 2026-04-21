using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Tokenization.Api.Health;

namespace Tokenization.Api.Controllers.Config;

/// <summary>
/// DI registration for the api layer.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Configures controllers with JSON handling.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void AddTokenizationControllers(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
    }

    /// <summary>
    /// Maps all application endpoints including controllers and health checks.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static void MapTokenizationEndpoints(this WebApplication app)
    {
        // Map health check endpoints with optimized response model
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var checks = new HealthCheckEntry[report.Entries.Count];
                var index = 0;
                foreach (var entry in report.Entries)
                {
                    checks[index] = new HealthCheckEntry
                    {
                        Name = entry.Key,
                        Status = entry.Value.Status.ToString(),
                        Duration = entry.Value.Duration.TotalMilliseconds,
                        Description = entry.Value.Description,
                        Data = entry.Value.Data.Count > 0
                            ? entry.Value.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                            : null
                    };
                    index++;
                }

                var response = new HealthCheckResponse
                {
                    Status = report.Status.ToString(),
                    Checks = checks,
                    TotalDuration = report.TotalDuration.TotalMilliseconds
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("database") || check.Tags.Contains("infrastructure")
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false // Only basic liveness check
        });

        // Map controllers
        app.MapControllers();
    }
}