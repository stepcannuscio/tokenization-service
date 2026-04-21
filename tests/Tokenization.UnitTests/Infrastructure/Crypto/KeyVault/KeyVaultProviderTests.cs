using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using FluentAssertions;
using Moq;
using System.Security.Cryptography;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Crypto.KeyVault;
using Tokenization.Infrastructure.Crypto.KeyVault.Mapping;
using Tokenization.Tests.Shared.Utils.Cache;
using Tokenization.Tests.Shared.Utils.KeyVault;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Crypto.KeyVault;

public class KeyVaultProviderTests
{
    [Fact]
    public async Task Wrap_Then_Unwrap_Roundtrip_Using_Current_Client()
    {
        const string keyId = "https://vault/keys/pay-kek/v1";
        var keyVaultKey = TestKeyVaultKey.New(keyId, "v1", DateTimeOffset.UtcNow);
        var cacheKey = TestCacheKey.New();
        var credential = new DefaultAzureCredential();

        var crypto = new Mock<CryptographyClient>(MockBehavior.Strict);
        byte[]? seenDek = null;

        crypto.Setup(c => c.WrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Callback<KeyWrapAlgorithm, byte[], CancellationToken>((_, dek, _) => seenDek = dek)
            .ReturnsAsync(CryptographyModelFactory.WrapResult(keyId, "\t\t"u8.ToArray(), KeyWrapAlgorithm.RsaOaep256));

        crypto.Setup(c => c.UnwrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CryptographyModelFactory.UnwrapResult(keyId, seenDek!, KeyWrapAlgorithm.RsaOaep256));

        var metadata = new KeyVaultKeyMetadata(keyId, credential, keyVaultKey.ToKeyVersionInfo()) { Client = crypto.Object };
        var cache = new Mock<IKeyClientCache<KeyVaultKeyMetadata, CryptographyClient>>();
        cache.Setup(c => c.GetCurrentClientAsync(cacheKey, CancellationToken.None)).ReturnsAsync(metadata);
        cache.Setup(c => c.GetAllClientsAsync(cacheKey, CancellationToken.None)).ReturnsAsync([metadata]);

        var metadataFactory = new KeyVaultKeyMetadataFactory(credential);

        var keyClient = new Mock<KeyClient>(MockBehavior.Strict); // not used by wrap/unwrap paths
        var provider = new KeyVaultProvider(keyClient.Object, cache.Object, metadataFactory);
        var dek = Convert.FromHexString("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F");

        var wrap = await provider.WrapKeyAsync(dek, cacheKey);
        var roundtrip = await provider.UnwrapKeyAsync(wrap.WrappedDek, cacheKey, wrap.KekKeyId);

        wrap.KekKeyId.Should().Be(keyId);
        roundtrip.Should().BeEquivalentTo(dek);
    }

    [Fact]
    public async Task Wrap_Uses_Current_When_Available()
    {
        const string keyId = "https://vault/keys/pay-kek/v1";
        var keyVaultKey = TestKeyVaultKey.New(keyId, "v1", DateTimeOffset.UtcNow);
        var cacheKey = TestCacheKey.New();
        var credential = new DefaultAzureCredential();

        var crypto = new Mock<CryptographyClient>(MockBehavior.Strict);
        crypto.Setup(c => c.WrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptographyModelFactory.WrapResult(keyId, [1], KeyWrapAlgorithm.RsaOaep256));
        
        var metadata = new KeyVaultKeyMetadata(keyId, credential, keyVaultKey.ToKeyVersionInfo()) { Client = crypto.Object };

        var cache = new Mock<IKeyClientCache<KeyVaultKeyMetadata, CryptographyClient>>(MockBehavior.Strict);
        cache.Setup(c => c.GetCurrentClientAsync(cacheKey, CancellationToken.None)).ReturnsAsync(metadata);
        cache.Setup(c => c.GetAllClientsAsync(cacheKey, CancellationToken.None)).ReturnsAsync([metadata]);

        var metadataFactory = new KeyVaultKeyMetadataFactory(credential);

        var provider = new KeyVaultProvider(new Mock<KeyClient>(MockBehavior.Strict).Object, cache.Object, metadataFactory);

        var payload = await provider.WrapKeyAsync(new byte[32], cacheKey);

        payload.KekKeyId.Should().Be(keyId);
    }

    [Fact]
    public async Task Wrap_FallsBack_To_Latest_When_No_Current()
    {
        var cacheKey = TestCacheKey.New();
        var credential = new DefaultAzureCredential();
        const string keyId1 = "https://vault/keys/pay-kek/v2";
        const string keyId2 = "https://vault/keys/pay-kek/v2";
        var keyVaultKey1 = TestKeyVaultKey.New(keyId1, "v1", DateTimeOffset.UtcNow.AddMinutes(-5));
        var keyVaultKey2 = TestKeyVaultKey.New(keyId2, "v2", DateTimeOffset.UtcNow);
        
        var metadata1 = new KeyVaultKeyMetadata(keyId1, credential, keyVaultKey1.ToKeyVersionInfo()) { Client = new Mock<CryptographyClient>().Object };

        var crypto2 = new Mock<CryptographyClient>(MockBehavior.Strict);
        crypto2.Setup(c => c.WrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptographyModelFactory.WrapResult(keyId2, [2], KeyWrapAlgorithm.RsaOaep256));
        
        var metadata2 = new KeyVaultKeyMetadata(keyId2, credential, keyVaultKey2.ToKeyVersionInfo()) { Client = crypto2.Object };

        var cache = new Mock<IKeyClientCache<KeyVaultKeyMetadata, CryptographyClient>>(MockBehavior.Strict);
        cache.Setup(c => c.GetCurrentClientAsync(cacheKey, CancellationToken.None)).ReturnsAsync((KeyVaultKeyMetadata?)null);
        cache.Setup(c => c.GetAllClientsAsync(cacheKey, CancellationToken.None)).ReturnsAsync([metadata1, metadata2]);

        var metadataFactory = new KeyVaultKeyMetadataFactory(credential);

        var provider = new KeyVaultProvider(new Mock<KeyClient>(MockBehavior.Strict).Object, cache.Object, metadataFactory);

        var payload = await provider.WrapKeyAsync(new byte[32], cacheKey);

        payload.KekKeyId.Should().Be(keyId2); // newest by CreatedAt
    }

    [Fact]
    public async Task Unwrap_Tries_Exact_KeyId_Then_Fallbacks_On_CryptographicException()
    {
        var cacheKey = TestCacheKey.New();
        var credential = new DefaultAzureCredential();
        const string keyId1 = "https://vault/keys/pay-kek/v2";
        const string keyId2 = "https://vault/keys/pay-kek/v2";
        var current = TestKeyVaultKey.New(keyId1, "v1", DateTimeOffset.UtcNow);
        var fallback = TestKeyVaultKey.New(keyId2, "v2", DateTimeOffset.UtcNow);

        var bad = new Mock<CryptographyClient>(MockBehavior.Strict);
        bad.Setup(c => c.UnwrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CryptographicException());

        var good = new Mock<CryptographyClient>(MockBehavior.Strict);
        var resultDek = Convert.FromHexString("00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEFF");
        good.Setup(
                c => c.UnwrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CryptographyModelFactory.UnwrapResult(keyId2, resultDek, KeyWrapAlgorithm.RsaOaep256));
        
        var currentMetadata = new KeyVaultKeyMetadata(keyId1, credential, current.ToKeyVersionInfo()) { Client = bad.Object };
        var fallbackMetadata = new KeyVaultKeyMetadata(keyId2, credential, fallback.ToKeyVersionInfo()) { Client = good.Object };

        var cache = new Mock<IKeyClientCache<KeyVaultKeyMetadata, CryptographyClient>>(MockBehavior.Strict);
        cache.Setup(c => c.GetClientAsync(cacheKey, keyId1, CancellationToken.None))
            .ReturnsAsync(currentMetadata); // fast path → throws
        cache.Setup(c => c.GetAllClientsAsync(cacheKey, CancellationToken.None))
            .ReturnsAsync([currentMetadata, fallbackMetadata]); // fallback → v2 OK

        var metadataFactory = new KeyVaultKeyMetadataFactory(credential);
       
        var provider = new KeyVaultProvider(new Mock<KeyClient>(MockBehavior.Strict).Object, cache.Object, metadataFactory);

        var unwrapped = await provider.UnwrapKeyAsync([5, 6], cacheKey, keyId1);

        unwrapped.Should().BeEquivalentTo(resultDek);
    }

    [Fact]
    public async Task WrapKeyAsync_Throws_When_No_Clients()
    {
        var cacheKey = TestCacheKey.New();
        var cred = new DefaultAzureCredential();

        var cache = new Mock<IKeyClientCache<KeyVaultKeyMetadata, CryptographyClient>>(MockBehavior.Strict);
        cache.Setup(c => c.GetCurrentClientAsync(cacheKey, CancellationToken.None)).ReturnsAsync((KeyVaultKeyMetadata?)null);
        cache.Setup(c => c.GetAllClientsAsync(cacheKey, CancellationToken.None)).ReturnsAsync([]);

        var metadataFactory = new KeyVaultKeyMetadataFactory(cred);
        
        var sut = new KeyVaultProvider(new Mock<KeyClient>(MockBehavior.Strict).Object, cache.Object, metadataFactory);

        await FluentActions.Invoking(() => sut.WrapKeyAsync(new byte[32], cacheKey))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No KEKs available*");
    }

    [Fact]
    public async Task UnwrapKeyAsync_Throws_When_No_Version_Succeeds()
    {
        const string keyId = "https://vault/keys/pay-kek/v1";
        var keyVaultKey = TestKeyVaultKey.New(keyId, "v1", DateTimeOffset.UtcNow);
        var cacheKey = TestCacheKey.New();
        var credential = new DefaultAzureCredential();

        var bad = new Mock<CryptographyClient>(MockBehavior.Strict);
        bad.Setup(c => c.UnwrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CryptographicException());
        
        var v1Metadata = new KeyVaultKeyMetadata(keyId, credential, keyVaultKey.ToKeyVersionInfo()) { Client = bad.Object };

        var cache = new Mock<IKeyClientCache<KeyVaultKeyMetadata, CryptographyClient>>(MockBehavior.Strict);
        cache.Setup(c => c.GetClientAsync(cacheKey, keyId, CancellationToken.None)).ReturnsAsync(v1Metadata);
        cache.Setup(c => c.GetAllClientsAsync(cacheKey, CancellationToken.None)).ReturnsAsync([v1Metadata]);

        var metadataFactory = new KeyVaultKeyMetadataFactory(credential);
        
        var sut = new KeyVaultProvider(new Mock<KeyClient>(MockBehavior.Strict).Object, cache.Object, metadataFactory);

        await FluentActions.Invoking(() => sut.UnwrapKeyAsync([7], cacheKey, keyId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unable to unwrap DEK*");
    }

    [Fact]
    public async Task SignDataAsync_With_Current_Client_Returns_HMAC_SHA256()
    {
        const string keyId = "https://vault/keys/pay-kek/v1";
        var keyVaultKey = TestKeyVaultKey.New(keyId, "v1", DateTimeOffset.UtcNow);
        var cacheKey = TestCacheKey.New();
        var credential = new DefaultAzureCredential();

        var crypto = new Mock<CryptographyClient>(MockBehavior.Strict);
        var expectedSignature = Convert.FromHexString("1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF");
        
        crypto.Setup(c => c.SignDataAsync(SignatureAlgorithm.RS256, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptographyModelFactory.SignResult(keyId, expectedSignature, SignatureAlgorithm.RS256));
        
        var clientMetadata = new KeyVaultKeyMetadata(keyId, credential, keyVaultKey.ToKeyVersionInfo()) { Client = crypto.Object };

        var cache = new Mock<IKeyClientCache<KeyVaultKeyMetadata, CryptographyClient>>();
        cache.Setup(c => c.GetCurrentClientAsync(cacheKey, CancellationToken.None)).ReturnsAsync(clientMetadata);
        cache.Setup(c => c.GetAllClientsAsync(cacheKey, CancellationToken.None)).ReturnsAsync([clientMetadata]);

        var metadataFactory = new KeyVaultKeyMetadataFactory(credential);
        
        var provider = new KeyVaultProvider(new Mock<KeyClient>(MockBehavior.Strict).Object, cache.Object, metadataFactory);
        var testData = "test-data-for-signing"u8.ToArray();

        var signature = await provider.SignDataAsync(testData, cacheKey, keyId);

        signature.Should().NotBeNull();
        signature.Length.Should().Be(32); // HMAC-SHA256 produces 32 bytes
        signature.Should().Equal(expectedSignature);
    }

    [Fact]
    public async Task SignDataAsync_With_Specific_KeyId_Uses_Exact_Client()
    {
        const string keyId1 = "https://vault/keys/pay-kek/v1";
        const string keyId2 = "https://vault/keys/pay-kek/v2";
        var keyVaultKey1 = TestKeyVaultKey.New(keyId1, "v1", DateTimeOffset.UtcNow.AddMinutes(-5));
        var keyVaultKey2 = TestKeyVaultKey.New(keyId2, "v2", DateTimeOffset.UtcNow);
        var cacheKey = TestCacheKey.New();
        var credential = new DefaultAzureCredential();

        var crypto1 = new Mock<CryptographyClient>(MockBehavior.Strict);
        var signature1 = Convert.FromHexString("1111111111111111111111111111111111111111111111111111111111111111");
        crypto1.Setup(c => c.SignDataAsync(SignatureAlgorithm.RS256, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptographyModelFactory.SignResult(keyId1, signature1, SignatureAlgorithm.RS256));

        var crypto2 = new Mock<CryptographyClient>(MockBehavior.Strict);
        var signature2 = Convert.FromHexString("2222222222222222222222222222222222222222222222222222222222222222");
        crypto2.Setup(c => c.SignDataAsync(SignatureAlgorithm.RS256, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptographyModelFactory.SignResult(keyId2, signature2, SignatureAlgorithm.RS256));
        
        var metadata1 = new KeyVaultKeyMetadata(keyId1, credential, keyVaultKey1.ToKeyVersionInfo()) { Client = crypto1.Object };
        var metadata2 = new KeyVaultKeyMetadata(keyId2, credential, keyVaultKey2.ToKeyVersionInfo()) { Client = crypto2.Object };

        var cache = new Mock<IKeyClientCache<KeyVaultKeyMetadata, CryptographyClient>>(MockBehavior.Strict);
        cache.Setup(c => c.GetClientAsync(cacheKey, keyId1, CancellationToken.None)).ReturnsAsync(metadata1);
        cache.Setup(c => c.GetCurrentClientAsync(cacheKey, CancellationToken.None)).ReturnsAsync(metadata2);
        cache.Setup(c => c.GetAllClientsAsync(cacheKey, CancellationToken.None)).ReturnsAsync([metadata1, metadata2]);

        var metadataFactory = new KeyVaultKeyMetadataFactory(credential);
        
        var provider = new KeyVaultProvider(new Mock<KeyClient>(MockBehavior.Strict).Object, cache.Object, metadataFactory);
        var testData = "test-data"u8.ToArray();

        // Sign with specific key ID (should use client1)
        var signatureWithKeyId = await provider.SignDataAsync(testData, cacheKey, keyId1);
        
        // Sign without key ID (should use current client2)
        var signatureWithoutKeyId = await provider.SignDataAsync(testData, cacheKey, null);

        signatureWithKeyId.Should().Equal(signature1);
        signatureWithoutKeyId.Should().Equal(signature2);
        signatureWithKeyId.Should().NotEqual(signatureWithoutKeyId);
    }

    [Fact]
    public async Task SignDataAsync_Throws_When_No_Clients_Available()
    {
        var cacheKey = TestCacheKey.New();
        var credential = new DefaultAzureCredential();

        var cache = new Mock<IKeyClientCache<KeyVaultKeyMetadata, CryptographyClient>>(MockBehavior.Strict);
        cache.Setup(c => c.GetCurrentClientAsync(cacheKey, CancellationToken.None)).ReturnsAsync((KeyVaultKeyMetadata?)null);
        cache.Setup(c => c.GetAllClientsAsync(cacheKey, CancellationToken.None)).ReturnsAsync([]);

        var metadataFactory = new KeyVaultKeyMetadataFactory(credential);
        var provider = new KeyVaultProvider(new Mock<KeyClient>(MockBehavior.Strict).Object, cache.Object, metadataFactory);

        await FluentActions.Invoking(() => provider.SignDataAsync("test"u8.ToArray(), cacheKey, null))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unable to sign data using any key*");
    }

    [Fact]
    public async Task SignDataAsync_With_Different_Data_Produces_Different_Signatures()
    {
        const string keyId = "https://vault/keys/pay-kek/v1";
        var keyVaultKey = TestKeyVaultKey.New(keyId, "v1", DateTimeOffset.UtcNow);
        var cacheKey = TestCacheKey.New();
        var credential = new DefaultAzureCredential();

        var crypto = new Mock<CryptographyClient>(MockBehavior.Strict);
        var signature1 = Convert.FromHexString("1111111111111111111111111111111111111111111111111111111111111111");
        var signature2 = Convert.FromHexString("2222222222222222222222222222222222222222222222222222222222222222");
        
        var callCount = 0;
        crypto.Setup(c => c.SignDataAsync(SignatureAlgorithm.RS256, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 
                    ? CryptographyModelFactory.SignResult(keyId, signature1, SignatureAlgorithm.RS256)
                    : CryptographyModelFactory.SignResult(keyId, signature2, SignatureAlgorithm.RS256);
            });
        
        var clientMetadata = new KeyVaultKeyMetadata(keyId, credential, keyVaultKey.ToKeyVersionInfo()) { Client = crypto.Object };

        var cache = new Mock<IKeyClientCache<KeyVaultKeyMetadata, CryptographyClient>>();
        cache.Setup(c => c.GetCurrentClientAsync(cacheKey, CancellationToken.None)).ReturnsAsync(clientMetadata);
        cache.Setup(c => c.GetAllClientsAsync(cacheKey, CancellationToken.None)).ReturnsAsync([clientMetadata]);

        var metadataFactory = new KeyVaultKeyMetadataFactory(credential);
        
        var provider = new KeyVaultProvider(new Mock<KeyClient>(MockBehavior.Strict).Object, cache.Object, metadataFactory);

        var data1 = "data1"u8.ToArray();
        var data2 = "data2"u8.ToArray();
        
        var result1 = await provider.SignDataAsync(data1, cacheKey, keyId);
        var result2 = await provider.SignDataAsync(data2, cacheKey, keyId);

        result1.Should().Equal(signature1);
        result2.Should().Equal(signature2);
        result1.Should().NotEqual(result2);
        result1.Length.Should().Be(32);
        result2.Length.Should().Be(32);
    }
}