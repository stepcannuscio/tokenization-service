using FluentAssertions;
using System.Net;
using System.Text.Json;
using Tokenization.Api.Health;
using Tokenization.Tests.Shared.Fixtures;
using Xunit;

namespace Tokenization.Tests.Integration.Api.Controllers.HealthController;

/// <summary>
/// Integration tests for the HealthController.
/// Tests health check endpoints, liveness, and readiness probes.
/// </summary>
public class HealthControllerIntegrationTests(WebApplicationFactoryFixture factory)
    : IClassFixture<WebApplicationFactoryFixture>
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task GetHealth_ShouldReturn200OK_WhenAllServicesHealthy()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);
    }

    [Fact]
    public async Task GetHealth_ShouldIncludeAllHealthChecks()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();
        var healthResponse = JsonSerializer.Deserialize<HealthCheckResponse>(content, _jsonOptions);

        // Assert
        healthResponse.Should().NotBeNull();
        healthResponse.Status.Should().Be("Healthy");
        healthResponse.Checks.Should().NotBeNullOrEmpty();
        healthResponse.TotalDuration.Should().BeGreaterThanOrEqualTo(0);

        // Verify that each health check has required properties.
        foreach (var check in healthResponse.Checks)
        {
            check.Name.Should().NotBeNullOrEmpty();
            check.Status.Should().NotBeNullOrEmpty();
            check.Duration.Should().BeGreaterThanOrEqualTo(0);
        }
        
        // Verify all expected health checks are included.
        var expectedHealthCheckNames = new List<string>
        {
            "api"
        };

        healthResponse.Checks.Select(c => c.Name).Should().Contain(expectedHealthCheckNames);
        healthResponse.Checks.Select(c => c.Name).Should().OnlyContain(name => expectedHealthCheckNames.Contains(name));
    }
    
    [Fact]
    public async Task GetHealth_ShouldWorkWithoutAuthentication()
    {
        // Arrange - Create client without any authentication headers
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Clear();

        // Act
        var response = await client.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);
    }
    
    [Fact]
    public async Task GetLiveness_ShouldReturn200OK()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/health/live");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrEmpty();

        var livenessResponse = JsonSerializer.Deserialize<HealthCheckResponse>(content, _jsonOptions);
        livenessResponse.Should().NotBeNull();
        livenessResponse.Status.Should().Be("alive");
    }

    [Fact]
    public async Task GetReadiness_ShouldReturn200OK()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/health/ready");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrEmpty();

        var readinessResponse = JsonSerializer.Deserialize<HealthCheckResponse>(content, _jsonOptions);
        readinessResponse.Should().NotBeNull();
        readinessResponse.Status.Should().Be("ready");
    }
}
