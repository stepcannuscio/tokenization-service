using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Tokenization.Infrastructure.Caching.Config.Options;
using Tokenization.Infrastructure.Caching.Health;
using Tokenization.Tests.Shared.Fixtures;
using Xunit;

namespace Tokenization.Tests.Integration.Cache;

public class CacheIntegrationTests : IClassFixture<HybridCacheFixture>
{
    private readonly Mock<ILogger<CacheHealthCheck>> _mockLogger;
    private readonly Mock<IOptions<CacheOptions>> _mockOptions;
    private readonly HybridCacheFixture _fixture;

    public CacheIntegrationTests(HybridCacheFixture fixture)
    {
        _mockLogger = new Mock<ILogger<CacheHealthCheck>>();
        _mockOptions = new Mock<IOptions<CacheOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(new CacheOptions());
        _fixture = fixture;
    }

    [Fact]
    public async Task CacheHealthCheck_WithHealthyRedis_ShouldReturnHealthy()
    {
        // Arrange
        var healthCheck = new CacheHealthCheck(_fixture.Cache!, _mockLogger.Object, _mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().NotBeNullOrEmpty();
        result.Data.Should().ContainKey("response_time_ms");
        result.Data["response_time_ms"].Should().BeOfType<long>();
    }

    [Fact]
    public async Task CacheHealthCheck_WithUnreachableRedis_ShouldReturnUnhealthy()
    {
        // Arrange
        const string invalidConnectionString = "localhost:9999,connectTimeout=100,syncTimeout=100,abortConnect=true";
        _fixture.SetServiceCollection(invalidConnectionString);
        var healthCheck = new CacheHealthCheck(_fixture.Cache!, _mockLogger.Object, _mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        _fixture.ResetServiceCollection();

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Cache");
        result.Exception.Should().NotBeNull();
    }
}
