using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Tokenization.Infrastructure.Caching.Config.Options;
using Tokenization.Infrastructure.Caching.Health;
using Tokenization.Tests.Shared.Fixtures;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Caching.Health;

public class CacheHealthCheckTests(HybridCacheFixtureInMemory fixture) : IClassFixture<HybridCacheFixtureInMemory>
{
    // Slow, !=

    private readonly Mock<ILogger<CacheHealthCheck>> _mockLogger = new();

    private static Mock<IOptions<CacheOptions>> ValidOptionsMock(int? healthCheckTimeoutSeconds = null)
    {
        var options = new CacheOptions();
        options.HealthCheckTimeoutSeconds = healthCheckTimeoutSeconds ?? options.HealthCheckTimeoutSeconds;
        var optionsMock = new Mock<IOptions<CacheOptions>>();
        optionsMock.Setup(x => x.Value).Returns(options);
        return optionsMock;
    }

    [Fact]
    public async Task CacheHealthCheck_WithValidCache_ShouldReturnHealthy()
    {
        // Arrange
        var healthCheck = new CacheHealthCheck(fixture.Cache, _mockLogger.Object, ValidOptionsMock().Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().NotBeNullOrEmpty();
        result.Data.Should().ContainKey("response_time_ms");
        result.Data["response_time_ms"].Should().BeOfType<long>();
    }

    [Fact]
    public async Task CacheHealthCheck_WithUnhealthyCache_ShouldReturnUnhealthy()
    {
        // Arrange
        fixture.SetInvalidCache();
        var healthCheck = new CacheHealthCheck(fixture.Cache, _mockLogger.Object, ValidOptionsMock().Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        fixture.SetValidCache();

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Cache");
    }
}
