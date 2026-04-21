using FluentAssertions;
using Tokenization.Infrastructure.Caching;
using Tokenization.Infrastructure.Crypto.Caching;
using Tokenization.Tests.Shared.Fixtures;
using Tokenization.Tests.Shared.Utils.Cache;
using Tokenization.Tests.Shared.Utils.Clients;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Crypto.Caching;

public class KeyClientCacheTests : IClassFixture<HybridCacheFixtureInMemory>
{
    private readonly KeyClientCache<TestKeyClient, string> _cache;

    public KeyClientCacheTests(HybridCacheFixtureInMemory fixture)
    {
        var keyGenerator = new CacheKeyGenerator();
        _cache = new KeyClientCache<TestKeyClient, string>(fixture.Cache, keyGenerator);
    }

    [Fact]
    public async Task GetAllClientsAsync_WhenEmpty_Returns_Empty()
    {
        var result = await _cache.GetAllClientsAsync(TestCacheKey.New());

        result.Should().BeEquivalentTo((IReadOnlyList<TestKeyClient>) []);
    }

    [Fact]
    public async Task SetClientsAsync_Persists_AllClients_In_Descending_CreatedAt_Order()
    {
        var now = DateTimeOffset.UtcNow;
        var c1 = TestKeyClient.Valid("kid/old", now.AddMinutes(-10));
        var c2 = TestKeyClient.Valid("kid/new", now.AddMinutes(-1), isCurrent: true);
        var c3 = TestKeyClient.Valid("kid/middle", now.AddMinutes(-5));
        var key = TestCacheKey.New();
        if (key.Length > 100) key = key[..100];

        await _cache.SetClientsAsync(key, [c1, c2, c3 ]);

        var all = await _cache.GetAllClientsAsync(key);
        all.Should().NotBeNull();
        all.Select(c => c.VersionInfo.KekKeyId)
            .Should().ContainInOrder("kid/new", "kid/middle", "kid/old");
    }

    [Fact]
    public async Task GetClientAsync_Returns_Matching_Client_By_KeyId()
    {
        var now = DateTimeOffset.UtcNow;
        var target = TestKeyClient.Valid("kid/target", now, isCurrent: true);
        var key = TestCacheKey.New();
        await _cache.SetClientsAsync(key, [TestKeyClient.Valid("kid/other", now.AddMinutes(-1)), target]);

        var found = await _cache.GetClientAsync(key, "kid/target");

        found.Should().NotBeNull();
        found.VersionInfo.KekKeyId.Should().Be("kid/target");
    }

    [Fact]
    public async Task GetClientAsync_When_NoClients_Exist_Should_Gracefully_Return_Null()
    {
        var key = TestCacheKey.New();
        if (key.Length > 100) key = key[..100];
        Func<Task> act = async () => _ = await _cache.GetClientAsync(key, "does-not-exist");

        await act.Should().NotThrowAsync("GetClientAsync should treat null as empty and return null");
        var result = await _cache.GetClientAsync(key, "does-not-exist");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentClientAsync_Returns_Current_Client_When_Set()
    {
        var now = DateTimeOffset.UtcNow;
        var current = TestKeyClient.Valid("kid/current", now, isCurrent: true);
        var older = TestKeyClient.Valid("kid/old", now.AddMinutes(-5));
        var key = TestCacheKey.New();
        await _cache.SetClientsAsync(key, [older, current]);

        var result = await _cache.GetCurrentClientAsync(key);

        result.Should().NotBeNull("the current client should be cached at the 'current' key");
        result.VersionInfo.KekKeyId.Should().Be("kid/current");
    }

    [Fact]
    public async Task GetCurrentClientAsync_When_No_Current_Is_Set_Returns_Null()
    {
        var now = DateTimeOffset.UtcNow;
        var key = TestCacheKey.New();
        await _cache.SetClientsAsync(key, [
            TestKeyClient.Valid("kid/a", now),
            TestKeyClient.Valid("kid/b", now.AddMinutes(-1)) /* neither current */
        ]);

        var result = await _cache.GetCurrentClientAsync(key);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetClientsAsync_Can_Overwrite_Previous_Clients()
    {
        var now = DateTimeOffset.UtcNow;
        var key = TestCacheKey.New();
        await _cache.SetClientsAsync(key, [TestKeyClient.Valid("kid/old", now.AddMinutes(-5), isCurrent: true)]);
        await _cache.SetClientsAsync(key, [TestKeyClient.Valid("kid/new", now, isCurrent: true)]);

        var all = await _cache.GetAllClientsAsync(key);
        var current = await _cache.GetCurrentClientAsync(key);

        all.Select(c => c.VersionInfo.KekKeyId).Should().ContainSingle("kid/new");
        current!.VersionInfo.KekKeyId.Should().Be("kid/new");
    }
}