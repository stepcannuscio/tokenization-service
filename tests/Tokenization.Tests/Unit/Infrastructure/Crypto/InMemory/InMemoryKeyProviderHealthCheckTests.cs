using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Tokenization.Infrastructure.Config.Options;
using Tokenization.Infrastructure.Crypto.Enums;
using Tokenization.Infrastructure.Crypto.InMemory;
using Tokenization.Tests.Shared.Utils.Crypto;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Crypto.InMemory;

public class InMemoryKeyProviderHealthCheckTests
{
    private readonly Mock<ILogger<InMemoryKeyProviderHealthCheck>> _mockLogger = new();

    private static Mock<IOptions<KeyStorageOptions>> ValidOptionsMock(int? healthCheckTimeoutSeconds = null)
    {
        var optionsMock = new Mock<IOptions<KeyStorageOptions>>();
        optionsMock.Setup(x => x.Value).Returns(new KeyStorageOptions
        {
            KeyProvider = KeyProviderType.InMemory,
            VaultUrl = "https://test-dummy.net/",
            KekKeyName = "test-kek",
            BlindIndexKeyName = "test-blind-index",
            EnableHealthChecks = true,
            HealthCheckTimeoutSeconds = healthCheckTimeoutSeconds ?? 1
        });

        return optionsMock;
    }
    
    [Fact]
    public async Task InMemoryKeyProviderHealthCheck_WithValidProvider_ShouldReturnHealthy()
    {
        // Arrange
        var healthCheck = new InMemoryKeyProviderHealthCheck(
            TestKeyProvider.ValidMock().Object, _mockLogger.Object, ValidOptionsMock().Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().NotBeNullOrEmpty();
        result.Data.Should().ContainKey("response_time_ms");
        result.Data["response_time_ms"].Should().BeOfType<long>();
    }

    [Fact]
    public async Task InMemoryKeyProviderHealthCheck_WithFailingProvider_ShouldReturnUnhealthy()
    {
        // Arrange
        var mockKeyProvider = TestKeyProvider.ValidMock();
        mockKeyProvider.Setup(kp => kp.PreloadKeysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Key provider failure"));

        var healthCheck = new InMemoryKeyProviderHealthCheck(
            mockKeyProvider.Object, _mockLogger.Object, ValidOptionsMock().Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("key provider");
    }
    
    [Fact]
    public async Task InMemoryKeyProviderHealthCheck_WithFailedWrapRoundTrip_ShouldReturnUnhealthy()
    {
        // Arrange
        var mockKeyProvider = TestKeyProvider.ValidMock();
        mockKeyProvider.Setup(kp => kp.UnwrapKeyAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        
        var healthCheck = new InMemoryKeyProviderHealthCheck(
            mockKeyProvider.Object, _mockLogger.Object, ValidOptionsMock().Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("wrap");
    }
        
    [Fact]
    public async Task InMemoryKeyProviderHealthCheck_WithEmptySignature_ShouldReturnUnhealthy()
    {
        // Arrange
        var mockKeyProvider = TestKeyProvider.ValidMock();
        mockKeyProvider.Setup(kp => kp.SignDataAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        
        var healthCheck = new InMemoryKeyProviderHealthCheck(
            mockKeyProvider.Object, _mockLogger.Object, ValidOptionsMock().Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("signature");
    }
            
    [Fact]
    public async Task InMemoryKeyProviderHealthCheck_WithSlowResponseTime_ShouldReturnDegraded()
    {
        // Arrange
        var mockKeyProvider = TestKeyProvider.ValidMock();
        mockKeyProvider.Setup(p => p.PreloadKeysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(1);
            });

        var mockOptions = ValidOptionsMock(healthCheckTimeoutSeconds: 0);
        
        var healthCheck = new InMemoryKeyProviderHealthCheck(
            mockKeyProvider.Object, _mockLogger.Object, mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("key provider");
    }
}