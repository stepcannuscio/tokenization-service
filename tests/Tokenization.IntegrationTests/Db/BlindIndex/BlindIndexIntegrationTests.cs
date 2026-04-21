using FluentAssertions;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Caching;
using Tokenization.Infrastructure.Crypto.Caching;
using Tokenization.Infrastructure.Crypto.InMemory;
using Tokenization.Infrastructure.Db.BlindIndex;
using Tokenization.Tests.Shared.Fixtures;
using Xunit;

namespace Tokenization.Tests.Integration.Db.BlindIndex;

/// <summary>
/// Integration tests for BlindIndexService using the default in-memory key provider.
/// This keeps the standard integration suite runnable without Azure dependencies.
/// </summary>
public class BlindIndexIntegrationTests
{
    [Fact]
    public async Task BlindIndexService_WithInMemoryProvider_ComputesConsistentHashes()
    {
        // Arrange
        using var cacheFixture = new HybridCacheFixtureInMemory();
        var keyProvider = CreateKeyProvider(cacheFixture);
        const string blindIndexKeyName = "blind-index-main";
        await keyProvider.PreloadKeysAsync(blindIndexKeyName);
        var blindIndexService = new BlindIndexService(keyProvider, blindIndexKeyName);

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
        using var cacheFixture = new HybridCacheFixtureInMemory();
        var keyProvider = CreateKeyProvider(cacheFixture);
        const string blindIndexKeyName = "blind-index-main";
        await keyProvider.PreloadKeysAsync(blindIndexKeyName);
        var blindIndexService = new BlindIndexService(keyProvider, blindIndexKeyName);

        // Act
        var hash1 = await blindIndexService.ComputeAsync("tenant-123", "v1");
        var hash2 = await blindIndexService.ComputeAsync("tenant-456", "v1");

        // Assert
        hash1.Should().NotBeEquivalentTo(hash2); // Different tenant IDs should produce different hashes
    }

    private static IKeyProvider CreateKeyProvider(HybridCacheFixtureInMemory cacheFixture)
    {
        var cache = new KeyClientCache<InMemoryKeyClient, byte[]>(
            cacheFixture.Cache,
            new CacheKeyGenerator());

        return new InMemoryKeyProvider(cache);
    }
}
