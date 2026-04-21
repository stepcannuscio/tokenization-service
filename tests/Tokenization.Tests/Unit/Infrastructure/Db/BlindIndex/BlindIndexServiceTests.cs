using FluentAssertions;
using Moq;
using System.Security.Cryptography;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Db.BlindIndex;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Db.BlindIndex;

public class BlindIndexServiceTests
{
    private static readonly byte[] V1Key = Convert.FromHexString(
        "00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEFF");

    private static readonly byte[] V2Key =
    [
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
    ];

    private static BlindIndexService NewService(IDictionary<string, byte[]>? keys = null)
    {
        var keyProvider = new Mock<IKeyProvider>();
        var keysDict = keys ?? new Dictionary<string, byte[]> { ["v1"] = V1Key };

        keyProvider.Setup(c => c.SignDataAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns<byte[], string, string?, CancellationToken>((data, _, keyId, _) =>
            {
                if (!keysDict.TryGetValue(keyId ?? string.Empty, out var key))
                    throw new InvalidOperationException();
                
                // Simulate HMAC-SHA256 computation using the key
                using var hmac = new HMACSHA256(key);
                return Task.FromResult(hmac.ComputeHash(data));
            });
        
        return new BlindIndexService(keyProvider.Object, "blind-index-key");
    }

    [Fact]
    public async Task Deterministic_For_Same_Input_And_KeyId()
    {
        var svc = NewService();

        var h1 = await svc.ComputeAsync("tenant-123", "v1");
        var h2 = await svc.ComputeAsync("tenant-123", "v1");

        h1.Should().NotBeNull();
        h2.Should().NotBeNull();
        h1.Should().Equal(h2);
        h1.Length.Should().Be(32); // 256-bit HMAC-SHA256
    }

    [Fact]
    public async Task Different_KeyIds_Produce_Different_Outputs()
    {
        var svc = NewService(new Dictionary<string, byte[]>
        {
            ["v1"] = V1Key,
            ["v2"] = V2Key
        });

        var v1 = await svc.ComputeAsync("tenant-123", "v1");
        var v2 = await svc.ComputeAsync("tenant-123", "v2");

        v1.Should().NotEqual(v2);
        v1.Should().NotBeNull();
        v2.Should().NotBeNull();
        v1.Length.Should().Be(32);
        v2.Length.Should().Be(32);
    }

    [Fact]
    public void Missing_KeyId_Throws()
    {
        var svc = NewService(new Dictionary<string, byte[]> { ["v1"] = V1Key });

        Action act = () => svc.ComputeAsync("tenant-123", "vX").GetAwaiter().GetResult();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Case_Sensitivity_Is_Preserved()
    {
        var svc = NewService();

        var lower = await svc.ComputeAsync("tenant-123", "v1");
        var upper = await svc.ComputeAsync("MERCHANT-123", "v1");

        lower.Should().NotEqual(upper);
        lower.Should().NotBeNull();
        upper.Should().NotBeNull();
        lower.Length.Should().Be(32);
        upper.Length.Should().Be(32);
    }

    [Fact]
    public async Task Different_Inputs_Produce_Different_Outputs()
    {
        var svc = NewService();

        var m = await svc.ComputeAsync("tenant-123", "v1");
        var c = await svc.ComputeAsync("customer-789", "v1");

        m.Should().NotEqual(c);
        m.Should().NotBeNull();
        c.Should().NotBeNull();
        m.Length.Should().Be(32);
        c.Length.Should().Be(32);
    }

    [Fact]
    public async Task Handles_NonStandard_Hash_Length()
    {
        // Test that BlindIndexService properly handles hash lengths that aren't exactly 32 bytes
        var keyProvider = new Mock<IKeyProvider>();
        
        keyProvider.Setup(c => c.SignDataAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns<byte[], string, string?, CancellationToken>((data, _, _, _) =>
            {
                // Simulate a hash that's longer than 32 bytes (should be truncated)
                using var hmac = new HMACSHA256(V1Key);
                var hash = hmac.ComputeHash(data);
                // Create a longer hash by duplicating some bytes
                var longHash = new byte[40];
                Array.Copy(hash, longHash, 32);
                Array.Copy(hash, 0, longHash, 32, 8);
                return Task.FromResult(longHash);
            });
        
        var svc = new BlindIndexService(keyProvider.Object, "blind-index-key");
        var result = await svc.ComputeAsync("test-value", "v1");
        
        result.Should().NotBeNull();
        result.Length.Should().Be(32); // Should be truncated to exactly 32 bytes
    }
}