using FluentAssertions;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.ValueObjects;
using Tokenization.Infrastructure.Crypto.Services;
using Tokenization.Tests.Shared.Utils.Cache;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Crypto.Services;

public class EncryptionServiceTests
{
    private const int NonceSize = 12; // must match service
    private const int TagSize = 16; // must match service

    [Fact]
    public async Task EncryptAsync_Produces_Correct_Sizes_And_Calls_Wrap()
    {
        const string plaintext = "test";
        var cacheKey = TestCacheKey.New();
        byte[]? wrapCalledWithDek = null;
        var returnedWrap = new KeyWrapPayload
        {
            WrappedDek = [0xAA, 0xBB],
            KekKeyId = "kid://current",
            Algorithm = "A256KW",
            WrappedAt = DateTimeOffset.UtcNow
        };

        var keyProvider = new Mock<IKeyProvider>(MockBehavior.Strict);
        keyProvider
            .Setup(k => k.WrapKeyAsync(It.IsAny<byte[]>(), It.IsAny<string>(),It.IsAny<CancellationToken>()))
            .Callback<byte[], string, CancellationToken>((dek, _, _) => wrapCalledWithDek = dek)
            .ReturnsAsync(returnedWrap);

        var service = new EncryptionService(keyProvider.Object, cacheKey);

        var result = await service.EncryptAsync(plaintext);

        result.Nonce.Should().HaveCount(NonceSize);
        result.Tag.Should().HaveCount(TagSize);
        result.Ciphertext.Should().HaveCount(Encoding.UTF8.GetByteCount(plaintext));
        result.WrapPayload.Should().BeSameAs(returnedWrap);
        keyProvider.Verify(k => k.WrapKeyAsync(It.IsAny<byte[]>(), It.IsAny<string>(),It.IsAny<CancellationToken>()), Times.Once);
        wrapCalledWithDek.Should().NotBeNull();
    }

    [Fact]
    public async Task EncryptAsync_Uses_Utf8_ByteLength_As_Ciphertext_Length()
    {
        var keyProvider = new Mock<IKeyProvider>();
        keyProvider
            .Setup(k => k.WrapKeyAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KeyWrapPayload { WrappedDek = [1], KekKeyId = "kid" });

        var cacheKey = TestCacheKey.New();
        var service = new EncryptionService(keyProvider.Object, cacheKey);
        const string plaintext = "h🎉llo-€-字"; // multibyte UTF-8

        var result = await service.EncryptAsync(plaintext);

        result.Ciphertext.Length.Should().Be(Encoding.UTF8.GetByteCount(plaintext));
    }

    [Fact]
    public async Task EncryptAsync_Generates_Random_Nonce_And_Tag()
    {
        var keyProvider = new Mock<IKeyProvider>();
        keyProvider
            .Setup(k => k.WrapKeyAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KeyWrapPayload { WrappedDek = [1], KekKeyId = "kid" });

        var cacheKey = TestCacheKey.New();
        var service = new EncryptionService(keyProvider.Object, cacheKey);

        var a = await service.EncryptAsync("same");
        var b = await service.EncryptAsync("same");

        a.Nonce.Should().NotEqual(b.Nonce);
        a.Tag.Should().NotEqual(b.Tag);
    }

    [Fact]
    public async Task EncryptAsync_Zeros_DEK_When_Wrap_Fails()
    {
        byte[]? capturedDekRef = null;
        var keyProvider = new Mock<IKeyProvider>();
        keyProvider
            .Setup(k => k.WrapKeyAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<byte[], string, CancellationToken>((dek, _, _) => capturedDekRef = dek)
            .ThrowsAsync(new InvalidOperationException("wrap failed"));

        var cacheKey = TestCacheKey.New();
        var service = new EncryptionService(keyProvider.Object, cacheKey);

        Func<Task> act = () => service.EncryptAsync("secret");

        await act.Should().ThrowAsync<InvalidOperationException>();
        capturedDekRef.Should().NotBeNull();
        capturedDekRef!.All(b => b == 0).Should().BeTrue("DEK must be zeroed even on failure");
    }

    [Fact]
    public async Task EncryptAsync_Ciphertext_Not_Plaintext()
    {
        var provider = new Mock<IKeyProvider>();
        provider
            .Setup(p => p.WrapKeyAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KeyWrapPayload { WrappedDek = [9], KekKeyId = "kid" });

        var cacheKey = TestCacheKey.New();
        var service = new EncryptionService(provider.Object, cacheKey);
        const string text = "super secret text";
        var result = await service.EncryptAsync(text);

        Encoding.UTF8.GetString(result.Ciphertext).Should().NotBe(text);
    }

    [Fact]
    public async Task DecryptAsync_RoundTrips_With_Unwrap_From_Provider()
    {
        // Capture DEK at wrap, but return a CLONE at unwrap (original is zeroized post-encrypt)
        byte[]? capturedDekRef = null;
        byte[]? savedDekForUnwrap = null;

        var wrapped = new byte[] { 0x10, 0x20 };
        const string keyId = "kid://current";
        const string plaintext = "roundtrip ✅";

        var provider = new Mock<IKeyProvider>(MockBehavior.Strict);
        provider
            .Setup(p => p.WrapKeyAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<byte[], string, CancellationToken>((dek, _, _) =>
            {
                capturedDekRef = dek; // same reference to check zeroization after encrypt
                savedDekForUnwrap = dek.ToArray(); // clone kept for later unwrap
            })
            .ReturnsAsync(new KeyWrapPayload { WrappedDek = wrapped, KekKeyId = keyId, Algorithm = "A256KW" });

        provider
            .Setup(p => p.UnwrapKeyAsync(wrapped, It.IsAny<string>(), keyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => savedDekForUnwrap!);

        var cacheKey = TestCacheKey.New();
        var service = new EncryptionService(provider.Object, cacheKey);

        var payload = await service.EncryptAsync(plaintext);
        var decrypted = await service.DecryptAsync(payload);

        capturedDekRef.Should().NotBeNull();
        capturedDekRef!.All(b => b == 0).Should().BeTrue();
        decrypted.Should().Be(plaintext);
        provider.Verify(p => p.UnwrapKeyAsync(wrapped, It.IsAny<string>(), keyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DecryptAsync_Bubbles_CryptographicException_On_Bad_Tag()
    {
        var knownDek = RandomNumberGenerator.GetBytes(32);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var pt = "cannot decrypt me"u8.ToArray();
        var cipher = new byte[pt.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(knownDek, TagSize))
        {
            aes.Encrypt(nonce, pt, cipher, tag);
        }

        // corrupt tag
        tag[0] ^= 0xFF;

        var wrapped = new byte[] { 0x33 };
        const string keyId = "kid://exact";

        var provider = new Mock<IKeyProvider>(MockBehavior.Strict);
        provider
            .Setup(p => p.UnwrapKeyAsync(wrapped, It.IsAny<string>(), keyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(knownDek);

        var cacheKey = TestCacheKey.New();
        var service = new EncryptionService(provider.Object, cacheKey);

        var payload = new EncryptedPayload
        {
            Ciphertext = cipher,
            Nonce = nonce,
            Tag = tag,
            WrapPayload = new KeyWrapPayload { WrappedDek = wrapped, KekKeyId = keyId, Algorithm = "A256KW" }
        };

        await FluentActions
            .Invoking(() => service.DecryptAsync(payload))
            .Should().ThrowAsync<CryptographicException>();
        knownDek.All(b => b == 0).Should().BeTrue("DEK must be zeroed in decrypt finally");
    }

    [Fact]
    public async Task DecryptAsync_Passes_KekKeyId_To_Unwrap()
    {
        var wrapped = new byte[] { 0x01, 0x02 };
        const string keyId = "kid://abc123";
        const string plaintext = "hello";

        var provider = new Mock<IKeyProvider>(MockBehavior.Strict);

        var dek = RandomNumberGenerator.GetBytes(32);
        provider
            .Setup(p => p.UnwrapKeyAsync(wrapped, It.IsAny<string>(), keyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dek);

        var cacheKey = TestCacheKey.New();
        var service = new EncryptionService(provider.Object, cacheKey);

        // Make ciphertext from known DEK so decrypt will succeed
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var pt = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[pt.Length];
        var tag = new byte[TagSize];
        using (var aes = new AesGcm(dek, TagSize))
        {
            aes.Encrypt(nonce, pt, cipher, tag);
        }

        var payload = new EncryptedPayload
        {
            Ciphertext = cipher,
            Nonce = nonce,
            Tag = tag,
            WrapPayload = new KeyWrapPayload { WrappedDek = wrapped, KekKeyId = keyId, Algorithm = "A256KW" }
        };

        var decrypted = await service.DecryptAsync(payload);

        decrypted.Should().Be(plaintext);
        provider.Verify(p => p.UnwrapKeyAsync(wrapped, It.IsAny<string>(), keyId, It.IsAny<CancellationToken>()), Times.Once);
        dek.All(b => b == 0).Should().BeTrue("DEK must be cleared after decrypt");
    }
}