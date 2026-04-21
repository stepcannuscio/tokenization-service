using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Tokenization.Api.Health;

namespace Tokenization.Api.Controllers;

/// <summary>
/// Controller for health check endpoints.
/// </summary>
[ApiController]
[Route("api/health")]
[Produces("application/json")]
public class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    /// <summary>
    /// Performs a comprehensive health check of all registered services.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health status of all services.</returns>
    /// <response code="200">All services are healthy.</response>
    /// <response code="503">One or more services are unhealthy.</response>
    [HttpGet]
    [ProducesResponseType<HealthCheckResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthCheckResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken = default)
    {
        var healthReport = await healthCheckService.CheckHealthAsync(cancellationToken);

        var response = new HealthCheckResponse
        {
            Status = healthReport.Status.ToString(),
            Checks = healthReport.Entries.Select(entry => new HealthCheckEntry
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Duration = entry.Value.Duration.TotalMilliseconds,
                Description = entry.Value.Description,
                Data = entry.Value.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            }).ToArray(),
            TotalDuration = healthReport.TotalDuration.TotalMilliseconds
        };

        var statusCode = healthReport.Status switch
        {
            HealthStatus.Healthy => StatusCodes.Status200OK,
            HealthStatus.Degraded => StatusCodes.Status200OK, // Degraded is still functional
            HealthStatus.Unhealthy => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status503ServiceUnavailable
        };

        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Performs a basic liveness check to verify the API is running.
    /// </summary>
    /// <returns>Simple alive status.</returns>
    /// <response code="200">API is alive and responding.</response>
    [HttpGet("live")]
    [ProducesResponseType<HealthCheckResponse>(StatusCodes.Status200OK)]
    public IActionResult GetLiveness()
    {
        return Ok(new HealthCheckResponse { Status = "alive" });
    }

    /// <summary>
    /// Performs a basic readiness check to verify the API is ready to serve requests.
    /// </summary>
    /// <returns>Simple ready status.</returns>
    /// <response code="200">API is ready to serve requests.</response>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetReadiness()
    {
        return Ok(new HealthCheckResponse { Status = "ready" });
    }
}