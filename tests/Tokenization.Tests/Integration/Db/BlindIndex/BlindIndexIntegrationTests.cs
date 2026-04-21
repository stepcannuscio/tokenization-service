using FluentAssertions;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Db.BlindIndex;
using Tokenization.Tests.Shared.Fixtures;
using Xunit;

namespace Tokenization.Tests.Integration.Db.BlindIndex;

/// <summary>
/// Integration tests for BlindIndexService with real KeyVaultProvider to ensure proper HMAC computation
/// and database integration with shadow properties.
/// </summary>
public class BlindIndexKeyVaultIntegrationTests(KeyVaultFixture keyVaultFixture)
    : IClassFixture<KeyVaultFixture>, IClassFixture<SqlServerFixture>
{
    [Fact]
    public async Task BlindIndexService_WithKeyVaultProvider_ComputesConsistentHashes()
    {
        // Arrange
        var keyVaultProvider = keyVaultFixture.GetKeyVaultProvider<IKeyProvider>();
        await keyVaultProvider.PreloadKeysAsync(keyVaultFixture.BlindIndexKeyName!);
        var blindIndexService = new BlindIndexService(keyVaultProvider, keyVaultFixture.BlindIndexKeyName!);

        // Act
        var hash1 = await blindIndexService.ComputeAsync("tenant-123", "v1");
        var hash2 = await blindIndexService.ComputeAsync("tenant-123", "v1");

        // Assert
        hash1.Should().NotBeNull();
        hash1.Should().HaveCount(32); // HMAC-SHA256 produces 32 bytes
        hash1.Should().BeEquivalentTo(hash2); // Should be deterministic
    }

    [Fact]
    public async Task BlindIndexService_WithDifferentValues_ProducesDifferentHashes()
    {
        // Arrange
        var keyVaultProvider = keyVaultFixture.GetKeyVaultProvider<IKeyProvider>();
        await keyVaultProvider.PreloadKeysAsync(keyVaultFixture.BlindIndexKeyName!);
        var blindIndexService = new BlindIndexService(keyVaultProvider, keyVaultFixture.BlindIndexKeyName!);

        // Act
        var hash1 = await blindIndexService.ComputeAsync("tenant-123", "v1");
        var hash2 = await blindIndexService.ComputeAsync("tenant-456", "v1");

        // Assert
        hash1.Should().NotBeEquivalentTo(hash2); // Different tenant IDs should produce different hashes
    }
}
