using Azure.Security.KeyVault.Keys;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Config.Options;
using Tokenization.Infrastructure.Crypto.Enums;
using Tokenization.Infrastructure.Crypto.KeyVault;
using Tokenization.Tests.Shared.Fixtures;
using Tokenization.Tests.Shared.Utils;
using Xunit;

namespace Tokenization.Tests.Integration.Crypto;

/// <summary>
/// Integration tests for Azure Key Vault functionality.
/// Tests real Key Vault operations including key wrapping, unwrapping, and health checks.
/// These tests are opt-in and require real Key Vault configuration.
/// </summary>
public class KeyVaultIntegrationTests : IClassFixture<KeyVaultFixture>
{
    private readonly KeyVaultFixture _fixture;
    private readonly KeyClient _keyClient;
    private readonly IKeyProvider _keyProvider;
    private readonly string _vaultUrl;
    private readonly string _kekKeyName;
    private readonly string _blindIndexKeyName;

    public KeyVaultIntegrationTests(KeyVaultFixture fixture)
    {
        _fixture = fixture;
        _keyClient = fixture.KeyClient ?? throw new NullReferenceException(nameof(_keyClient));
        _keyProvider = _fixture.GetKeyVaultProvider<IKeyProvider>() ??
                       throw new NullReferenceException(nameof(_fixture.GetKeyVaultProvider));
        _vaultUrl = fixture.VaultUrl ?? throw new NullReferenceException(nameof(_vaultUrl));
        _kekKeyName = fixture.KekKeyName ?? throw new NullReferenceException(nameof(_kekKeyName));
        _blindIndexKeyName = fixture.BlindIndexKeyName ?? throw new NullReferenceException(nameof(_blindIndexKeyName));
    }

    [KeyVaultFact]
    public async Task KeyVaultHealthCheck_WithHealthyKeyVaultAndCache_ShouldReturnHealthy()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var logger = new LoggerFactory().CreateLogger<KeyVaultHealthCheck>();

        // Create KeyStorageOptions for the health check
        var keyStorageOptions = new KeyStorageOptions
        {
            KeyProvider = KeyProviderType.AzureKeyVault,
            VaultUrl = _vaultUrl,
            KekKeyName = _kekKeyName,
            BlindIndexKeyName = _blindIndexKeyName,
            EnableHealthChecks = true,
            HealthCheckTimeoutSeconds = 10
        };

        var healthCheck = new KeyVaultHealthCheck(
            _keyClient,
            _keyProvider,
            logger,
            Options.Create(keyStorageOptions));

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().NotBeNullOrEmpty();
        result.Data.Should().ContainKey("response_time_ms");
        result.Data["response_time_ms"].Should().BeOfType<long>();
    }

    [KeyVaultFact]
    public async Task KeyVaultHealthCheck_WithInvalidKeyName_ShouldReturnUnhealthy()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var logger = new LoggerFactory().CreateLogger<KeyVaultHealthCheck>();

        // Create KeyStorageOptions with invalid key name to test unhealthy scenario
        var keyStorageOptions = new KeyStorageOptions
        {
            KeyProvider = KeyProviderType.AzureKeyVault,
            VaultUrl = _vaultUrl,
            KekKeyName = "kek-name-that-doesnt-exist",
            BlindIndexKeyName = _blindIndexKeyName,
            EnableHealthChecks = true,
            HealthCheckTimeoutSeconds = 10
        };

        var healthCheck = new KeyVaultHealthCheck(
            _keyClient,
            _keyProvider,
            logger,
            Options.Create(keyStorageOptions));

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Key Vault");
    }

    [KeyVaultFact]
    public async Task KeyVaultProvider_Wrap_Unwrap_Completes_RoundTrip()
    {
        // Arrange
        await _keyProvider.PreloadKeysAsync(_kekKeyName);
        var testData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        // Act
        var wrapped = await _keyProvider.WrapKeyAsync(testData, _kekKeyName);
        var unwrapped = await _keyProvider.UnwrapKeyAsync(wrapped.WrappedDek, _kekKeyName, wrapped.KekKeyId);

        // Assert
        unwrapped.Should().BeEquivalentTo(testData);
    }

    [KeyVaultFact]
    public async Task KeyVaultProvider_SignData_ShouldSucceed()
    {
        // Arrange
        await _keyProvider.PreloadKeysAsync(_kekKeyName);
        var testData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        // Act
        var signature = await _keyProvider.SignDataAsync(testData, _kekKeyName, null);

        // Assert
        signature.Should().NotBeNull();
        signature.Should().NotBeEmpty();
        signature.Length.Should().BeGreaterThan(0);
    }
}
