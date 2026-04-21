using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Caching;
using Tokenization.Infrastructure.Crypto.Caching;
using Tokenization.Infrastructure.Crypto.InMemory;
using Tokenization.Tests.Shared.Fixtures;
using Tokenization.Tests.Shared.Utils.Cache;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Crypto.InMemory;

public class InMemoryKeyProviderTests
{
    private static byte[] Hex(string s) => Convert.FromHexString(s);

    private static (InMemoryKeyClient client, string keyId) Client(
        string keyName, int version, bool isCurrent, byte[] kek, DateTimeOffset? created = null)
    {
        var c = new InMemoryKeyClient(keyName, version, isCurrent)
        {
            Client = kek
        };
        c.VersionInfo = c.VersionInfo with { CreatedAt = created ?? DateTimeOffset.UtcNow.AddMinutes(version) };
        return (c, c.VersionInfo.KekKeyId);
    }

    private static byte[] WrapWithKek(byte[] kek, byte[] dek)
    {
        using var aes = Aes.Create();
        aes.Key = kek;
        aes.GenerateIV();
        using var enc = aes.CreateEncryptor();
        var cipher = enc.TransformFinalBlock(dek, 0, dek.Length);
        return aes.IV.Concat(cipher).ToArray(); // IV || C
    }

    [Fact]
    public async Task Wrap_Then_Unwrap_Roundtrip_With_Known_KEK()
    {
        var cacheKey = TestCacheKey.New();
        var keyId = $"inmemory://keys/{cacheKey}/v0001";
        var knownKek = Convert.FromHexString(
            "A0A1A2A3A4A5A6A7A8A9AAABACADAEAFB0B1B2B3B4B5B6B7B8B9BABBBCBDBEBF");

        // Seed a current client with known KEK
        var client = new InMemoryKeyClient(cacheKey, version: 1, isCurrent: true)
        {
            Client = knownKek
        };

        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.GetCurrentClientAsync(cacheKey, CancellationToken.None)).ReturnsAsync(client);
        cache.Setup(c => c.GetAllClientsAsync(cacheKey, CancellationToken.None)).ReturnsAsync([client]);
        cache.Setup(c => c.GetClientAsync(cacheKey, keyId, CancellationToken.None)).ReturnsAsync(client);

        var provider = new InMemoryKeyProvider(cache.Object);

        // Known DEK (32 bytes)
        var dek = Convert.FromHexString(
            "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F");

        var wrapped = await provider.WrapKeyAsync(dek, cacheKey);
        var roundtrip = await provider.UnwrapKeyAsync(wrapped.WrappedDek, cacheKey, wrapped.KekKeyId);

        wrapped.KekKeyId.Should().Be(keyId);
        wrapped.Algorithm.Should().Be("AES-CBC-DEV");
        wrapped.WrappedDek.Should().NotBeNull();
        wrapped.WrappedDek.Length.Should().BeGreaterThan(16); // IV(16) + cipher
        roundtrip.Should().BeEquivalentTo(dek);
    }

    [Fact]
    public async Task Wrap_Uses_Current_Client_When_Available()
    {
        var key = TestCacheKey.New();
        var kekCurrent = Hex("1111111111111111111111111111111111111111111111111111111111111111");
        var (current, currentId) = Client(key, 2, true, kekCurrent);

        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.GetCurrentClientAsync(key, CancellationToken.None)).ReturnsAsync(current);
        cache.Setup(c => c.GetAllClientsAsync(key, CancellationToken.None)).ReturnsAsync([current]);

        var provider = new InMemoryKeyProvider(cache.Object);

        var payload = await provider.WrapKeyAsync(new byte[32], key);

        payload.KekKeyId.Should().Be(currentId);
        payload.Algorithm.Should().Be("AES-CBC-DEV");
    }

    [Fact]
    public async Task Wrap_FallsBack_To_Latest_When_No_Current()
    {
        var key = TestCacheKey.New();
        var kekOld = Hex("2222222222222222222222222222222222222222222222222222222222222222");
        var kekNew = Hex("3333333333333333333333333333333333333333333333333333333333333333");

        var (oldClient, _) = Client(key, 1, false, kekOld, DateTimeOffset.UtcNow.AddMinutes(-5));
        var (newClient, newId) = Client(key, 2, false, kekNew, DateTimeOffset.UtcNow);

        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.GetCurrentClientAsync(key, CancellationToken.None)).ReturnsAsync((InMemoryKeyClient?)null);
        cache.Setup(c => c.GetAllClientsAsync(key, CancellationToken.None)).ReturnsAsync([oldClient, newClient]);

        var provider = new InMemoryKeyProvider(cache.Object);

        var payload = await provider.WrapKeyAsync(new byte[32], key);

        payload.KekKeyId.Should().Be(newId);
    }

    [Fact]
    public async Task Wrap_Throws_When_No_Clients_Available()
    {
        var key = TestCacheKey.New();
        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.GetCurrentClientAsync(key, CancellationToken.None)).ReturnsAsync((InMemoryKeyClient?)null);
        cache.Setup(c => c.GetAllClientsAsync(key, CancellationToken.None)).ReturnsAsync([]);

        var sut = new InMemoryKeyProvider(cache.Object);

        await FluentActions.Invoking(() => sut.WrapKeyAsync(new byte[32], key))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No KEKs available*");
    }

    [Fact]
    public async Task Unwrap_Tries_Exact_KeyId_First_Then_Fallbacks_On_CryptographicException()
    {
        var key = TestCacheKey.New();
        var dek = Hex("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F");

        // v1 (wrong): will fail to unwrap
        var kekWrong = Hex("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        var (v1, v1Id) = Client(key, 1, false, kekWrong, DateTimeOffset.UtcNow.AddMinutes(-2));

        // v2 (right): will successfully unwrap
        var kekRight = Hex("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
        var (v2, _) = Client(key, 2, false, kekRight, DateTimeOffset.UtcNow);

        // Build a wrapped DEK using the RIGHT KEK (v2)
        var wrapped = WrapWithKek(kekRight, dek);

        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.GetClientAsync(key, v1Id, CancellationToken.None)).ReturnsAsync(v1); // fast path → will throw inside provider
        cache.Setup(c => c.GetAllClientsAsync(key, CancellationToken.None)).ReturnsAsync([v1, v2]);

        var provider = new InMemoryKeyProvider(cache.Object);

        // Unwrap using the WRONG KEK (v1)
        var unwrapped = await provider.UnwrapKeyAsync(wrapped, key, v1Id);

        unwrapped.Should().BeEquivalentTo(dek);
    }

    [Fact]
    public async Task Unwrap_Throws_When_No_Clients_Available()
    {
        var key = TestCacheKey.New();
        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.GetClientAsync(key, It.IsAny<string>(), CancellationToken.None)).ReturnsAsync((InMemoryKeyClient?)null);
        cache.Setup(c => c.GetAllClientsAsync(key, CancellationToken.None)).ReturnsAsync([]);

        var provider = new InMemoryKeyProvider(cache.Object);

        await FluentActions.Invoking(() => provider.UnwrapKeyAsync(new byte[16], key, "some-id"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unable to unwrap DEK using any KEK*");
    }

    [Fact]
    public async Task PreloadKeysAsync_ShouldInsertFirstClient_And_SetCurrent()
    {
        var key = TestCacheKey.New();

        IReadOnlyList<InMemoryKeyClient>? allClientsSet = null;
        InMemoryKeyClient? currentClientSet = null;

        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.SetClientsAsync(key, It.IsAny<IReadOnlyList<InMemoryKeyClient>>(), CancellationToken.None))
            .Callback<string, IReadOnlyList<InMemoryKeyClient>, CancellationToken>((_, value, _) =>
                allClientsSet = value)
            .Returns(Task.CompletedTask);
        cache.Setup(c => c.GetAllClientsAsync(key, CancellationToken.None)).ReturnsAsync(() => allClientsSet ?? []);
        cache.Setup(c => c.GetCurrentClientAsync(key, CancellationToken.None)).ReturnsAsync(() => currentClientSet);

        var provider = new InMemoryKeyProvider(cache.Object);

        await provider.PreloadKeysAsync(key);

        allClientsSet.Should().NotBeNull();
        allClientsSet.Should().HaveCount(1);

        var only = allClientsSet.Single();
        only.VersionInfo.KekKeyId.Should().Be($"inmemory://keys/{key}/v0001");
        only.VersionInfo.IsCurrent.Should().BeTrue();
        only.Client.Should().HaveCount(32);
    }

    [Fact]
    public async Task PreloadKeysAsync_ShouldNotReplaceExistingClients()
    {
        var key = TestCacheKey.New();
        var existingClient = new InMemoryKeyClient(key, 1, true);
        var existingClients = (IReadOnlyList<InMemoryKeyClient>)[existingClient];

        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.GetAllClientsAsync(key, CancellationToken.None)).ReturnsAsync(existingClients);

        var provider = new InMemoryKeyProvider(cache.Object);

        await provider.PreloadKeysAsync(key);

        cache.Verify(
            c => c.SetClientsAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<InMemoryKeyClient>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReloadKeysAsync_IsNoOp_And_DoesNotChangeClients()
    {
        var key = TestCacheKey.New();
        var client = new InMemoryKeyClient(key, 1, true);

        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.GetAllClientsAsync(key, CancellationToken.None)).ReturnsAsync([client]);
        cache.Setup(c => c.GetCurrentClientAsync(key, CancellationToken.None)).ReturnsAsync(client);

        var provider = new InMemoryKeyProvider(cache.Object);

        await provider.PreloadKeysAsync(key);

        // (no-op for InMemoryProvider)
        await provider.ReloadKeysAsync(key);

        // Verify that ReloadKeysAsync doesn't call any cache methods (it's a no-op)
        cache.Verify(
            c => c.SetClientsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<InMemoryKeyClient>>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RotateKeyAsync_ShouldAdd_New_Version()
    {
        var key = TestCacheKey.New();

        IReadOnlyList<InMemoryKeyClient>? allClientsSet = null;
        InMemoryKeyClient? currentClientSet = null;

        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.SetClientsAsync(key, It.IsAny<IReadOnlyList<InMemoryKeyClient>>(), CancellationToken.None))
            .Callback<string, IReadOnlyList<InMemoryKeyClient>, CancellationToken>((_, value, _) =>
                allClientsSet = value)
            .Returns(Task.CompletedTask);
        cache.Setup(c => c.GetAllClientsAsync(key, CancellationToken.None)).ReturnsAsync(() => allClientsSet ?? []);
        cache.Setup(c => c.GetCurrentClientAsync(key, CancellationToken.None)).ReturnsAsync(() => currentClientSet);

        var provider = new InMemoryKeyProvider(cache.Object);
        await provider.PreloadKeysAsync(key);

        await provider.RotateKeyAsync(key);

        allClientsSet.Should().NotBeNull();
        allClientsSet.Should().HaveCount(2);

        // V2 exists and is marked current
        allClientsSet.Any(c => c.VersionInfo.KekKeyId.EndsWith("/v0002") && c.VersionInfo.IsCurrent).Should().BeTrue();
    }

    [Fact]
    public async Task SignDataAsync_With_Current_Client_Returns_HMAC_SHA256()
    {
        var key = TestCacheKey.New();
        var knownKek = Convert.FromHexString(
            "A0A1A2A3A4A5A6A7A8A9AAABACADAEAFB0B1B2B3B4B5B6B7B8B9BABBBCBDBEBF");

        var client = new InMemoryKeyClient(key, version: 1, isCurrent: true)
        {
            Client = knownKek
        };

        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.GetCurrentClientAsync(key, CancellationToken.None)).ReturnsAsync(client);
        cache.Setup(c => c.GetAllClientsAsync(key, CancellationToken.None)).ReturnsAsync([client]);

        var provider = new InMemoryKeyProvider(cache.Object);

        var testData = "test-data-for-signing"u8.ToArray();
        var signature = await provider.SignDataAsync(testData, key, null);

        signature.Should().NotBeNull();
        signature.Length.Should().Be(32); // HMAC-SHA256 produces 32 bytes

        // Verify it's deterministic
        var signature2 = await provider.SignDataAsync(testData, key, null);
        signature.Should().Equal(signature2);
    }

    [Fact]
    public async Task SignDataAsync_With_Specific_KeyId_Uses_Exact_Client()
    {
        var key = TestCacheKey.New();
        var kek1 = Convert.FromHexString("1111111111111111111111111111111111111111111111111111111111111111");
        var kek2 = Convert.FromHexString("2222222222222222222222222222222222222222222222222222222222222222");

        var client1 = new InMemoryKeyClient(key, version: 1, isCurrent: false)
        {
            Client = kek1
        };
        client1.VersionInfo = client1.VersionInfo with { CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5) };

        var client2 = new InMemoryKeyClient(key, version: 2, isCurrent: true)
        {
            Client = kek2
        };

        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.GetClientAsync(key, client1.VersionInfo.KekKeyId, CancellationToken.None))
            .ReturnsAsync(client1);
        cache.Setup(c => c.GetCurrentClientAsync(key, CancellationToken.None)).ReturnsAsync(client2);
        cache.Setup(c => c.GetAllClientsAsync(key, CancellationToken.None)).ReturnsAsync([client1, client2]);

        var provider = new InMemoryKeyProvider(cache.Object);

        var testData = "test-data"u8.ToArray();

        // Sign with specific key ID (should use client1)
        var signatureWithKeyId = await provider.SignDataAsync(testData, key, client1.VersionInfo.KekKeyId);

        // Sign without key ID (should use current client2)
        var signatureWithoutKeyId = await provider.SignDataAsync(testData, key, null);

        signatureWithKeyId.Should().NotEqual(signatureWithoutKeyId);
        signatureWithKeyId.Length.Should().Be(32);
        signatureWithoutKeyId.Length.Should().Be(32);
    }

    [Fact]
    public async Task SignDataAsync_Throws_When_No_Clients_Available()
    {
        var key = TestCacheKey.New();
        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.GetCurrentClientAsync(key, CancellationToken.None)).ReturnsAsync((InMemoryKeyClient?)null);
        cache.Setup(c => c.GetAllClientsAsync(key, CancellationToken.None)).ReturnsAsync([]);

        var provider = new InMemoryKeyProvider(cache.Object);

        await FluentActions.Invoking(() => provider.SignDataAsync("test"u8.ToArray(), key, null))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unable to sign data using any key*");
    }

    [Fact]
    public async Task SignDataAsync_With_Different_Data_Produces_Different_Signatures()
    {
        var key = TestCacheKey.New();
        var knownKek = Convert.FromHexString(
            "A0A1A2A3A4A5A6A7A8A9AAABACADAEAFB0B1B2B3B4B5B6B7B8B9BABBBCBDBEBF");

        var client = new InMemoryKeyClient(key, version: 1, isCurrent: true)
        {
            Client = knownKek
        };

        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.GetCurrentClientAsync(key, CancellationToken.None)).ReturnsAsync(client);
        cache.Setup(c => c.GetAllClientsAsync(key, CancellationToken.None)).ReturnsAsync([client]);

        var provider = new InMemoryKeyProvider(cache.Object);

        var data1 = "data1"u8.ToArray();
        var data2 = "data2"u8.ToArray();

        var signature1 = await provider.SignDataAsync(data1, key, null);
        var signature2 = await provider.SignDataAsync(data2, key, null);

        signature1.Should().NotEqual(signature2);
        signature1.Length.Should().Be(32);
        signature2.Length.Should().Be(32);
    }

    [Fact]
    public async Task SignDataAsync_Produces_Consistent_HMAC_SHA256_Results()
    {
        var key = TestCacheKey.New();
        var knownKek = Convert.FromHexString(
            "00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEFF");

        var client = new InMemoryKeyClient(key, version: 1, isCurrent: true)
        {
            Client = knownKek
        };

        var cache = new Mock<IKeyClientCache<InMemoryKeyClient, byte[]>>(MockBehavior.Strict);
        cache.Setup(c => c.GetCurrentClientAsync(key, CancellationToken.None)).ReturnsAsync(client);
        cache.Setup(c => c.GetAllClientsAsync(key, CancellationToken.None)).ReturnsAsync([client]);

        var provider = new InMemoryKeyProvider(cache.Object);

        var testData = "test-data-for-hmac"u8.ToArray();
        var signature = await provider.SignDataAsync(testData, key, null);

        // Verify the signature matches expected HMAC-SHA256 computation
        using var hmac = new HMACSHA256(knownKek);
        var expectedSignature = hmac.ComputeHash(testData);

        signature.Should().Equal(expectedSignature);
        signature.Length.Should().Be(32);
    }

    [Fact]
    public async Task Wrap_Then_Unwrap_Roundtrip_With_Real_HybridCache()
    {
        using var cacheFixture = new HybridCacheFixtureInMemory();
        var cache = new KeyClientCache<InMemoryKeyClient, byte[]>(cacheFixture.Cache, new CacheKeyGenerator());
        var provider = new InMemoryKeyProvider(cache);
        var key = TestCacheKey.New();
        var dek = RandomNumberGenerator.GetBytes(32);

        await provider.PreloadKeysAsync(key);
        var wrapped = await provider.WrapKeyAsync(dek, key);
        var roundtrip = await provider.UnwrapKeyAsync(wrapped.WrappedDek, key, wrapped.KekKeyId);

        roundtrip.Should().Equal(dek);
    }

    [Fact]
    public async Task Wrap_Then_Unwrap_Roundtrip_With_DistributedMemoryHybridCache()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddDistributedMemoryCache();
        services.AddHybridCache();

        await using var serviceProvider = services.BuildServiceProvider();
        var hybridCache = serviceProvider.GetRequiredService<Microsoft.Extensions.Caching.Hybrid.HybridCache>();
        var cache = new KeyClientCache<InMemoryKeyClient, byte[]>(hybridCache, new CacheKeyGenerator());
        var provider = new InMemoryKeyProvider(cache);
        var key = TestCacheKey.New();
        var dek = RandomNumberGenerator.GetBytes(32);

        await provider.PreloadKeysAsync(key);
        var wrapped = await provider.WrapKeyAsync(dek, key);
        var roundtrip = await provider.UnwrapKeyAsync(wrapped.WrappedDek, key, wrapped.KekKeyId);

        roundtrip.Should().Equal(dek);
    }
}
