using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Tokenization.Infrastructure.Config.Options;
using Tokenization.Infrastructure.Crypto.Enums;
using Tokenization.Infrastructure.Crypto.KeyVault;
using Tokenization.Tests.Shared.Utils.Crypto;
using Tokenization.Tests.Shared.Utils.KeyVault;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Crypto.KeyVault;

public class KeyVaultHealthCheckTests
{
    private readonly Mock<ILogger<KeyVaultHealthCheck>> _mockLogger = new();

    private static Mock<IOptions<KeyStorageOptions>> ValidOptionsMock(int? healthCheckTimeoutSeconds = null)
    {
        var optionsMock = new Mock<IOptions<KeyStorageOptions>>();
        optionsMock.Setup(x => x.Value).Returns(new KeyStorageOptions
        {
            KeyProvider = KeyProviderType.AzureKeyVault,
            VaultUrl = "https://test-vault.vault.azure.net/",
            KekKeyName = "test-kek",
            BlindIndexKeyName = "test-blind-index",
            EnableHealthChecks = true,
            HealthCheckTimeoutSeconds = healthCheckTimeoutSeconds ?? 1
        });

        return optionsMock;
    }

    [Fact]
    public async Task KeyVaultHealthCheck_WithValidProvider_ShouldReturnHealthy()
    {
        // Arrange
        var healthCheck = new KeyVaultHealthCheck(
            TestKeyClient.ValidMock().Object,
            TestKeyProvider.ValidMock().Object,
            _mockLogger.Object,
            ValidOptionsMock().Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().NotBeNullOrEmpty();
        result.Data.Should().ContainKey("response_time_ms");
        result.Data["response_time_ms"].Should().BeOfType<long>();
    }

    [Fact]
    public async Task KeyVaultHealthCheck_WithUnreachableVault_ShouldReturnUnhealthy()
    {
        // Arrange
        var mockKeyProvider = TestKeyProvider.ValidMock();
        mockKeyProvider.Setup(kp => kp.PreloadKeysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Key Vault unreachable"));

        var healthCheck = new KeyVaultHealthCheck(
            TestKeyClient.ValidMock().Object,
            mockKeyProvider.Object,
            _mockLogger.Object,
            ValidOptionsMock().Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Key Vault");
    }


    [Fact]
    public async Task KeyVaultHealthCheck_WithSlowResponseTime_ShouldReturnDegraded()
    {
        // Arrange
        var mockKeyProvider = TestKeyProvider.ValidMock();
        mockKeyProvider.Setup(p => p.PreloadKeysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(1);
            });

        var mockOptions = ValidOptionsMock(healthCheckTimeoutSeconds: 0);

        var healthCheck = new KeyVaultHealthCheck(
            TestKeyClient.ValidMock().Object,
            mockKeyProvider.Object,
            _mockLogger.Object,
            mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Key Vault");
    }
}
