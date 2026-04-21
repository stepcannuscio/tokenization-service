using FluentAssertions;
using System.Security.Cryptography;
using System.Text;
using Tokenization.Api.Idempotency;
using Xunit;

namespace Tokenization.Tests.Unit.Api.Idempotency;

/// <summary>
/// Unit tests for the DefaultIdempotencyKeyHasher to ensure proper cache key generation.
/// </summary>
public sealed class DefaultIdempotencyKeyHasherTests
{
    private readonly DefaultIdempotencyKeyHasher _hasher = new();

    [Theory]
    [InlineData("user-123", "POST", "/api/tokens", "test-key-123")]
    [InlineData("", "", "", "")]
    [InlineData("user@domain.com", "POST", "/api/tokens?param=value&other=123", "key-with-special-chars!@#$%^&*()")]
    [InlineData("用户-123", "POST", "/api/令牌", "测试-键-123")]
    public void Hash_WithValidInputs_ShouldReturnPrefixedHash(string partition, string method, string path, string idempotencyKey)
    {
        // Act
        var result = _hasher.Hash(partition, method, new PathString(path), idempotencyKey);

        // Assert
        result.Should().StartWith("idem:");
        result.Should().HaveLength(69); // "idem:" + 64 hex characters
        result.Should().MatchRegex("^idem:[A-Fa-f0-9]{64}$");
    }

    [Fact]
    public void Hash_WithSameInputs_ShouldReturnIdenticalHashes()
    {
        // Arrange
        const string partition = "user-123";
        const string method = "POST";
        var path = new PathString("/api/tokens");
        const string idempotencyKey = "test-key-123";

        // Act
        var result1 = _hasher.Hash(partition, method, path, idempotencyKey);
        var result2 = _hasher.Hash(partition, method, path, idempotencyKey);

        // Assert
        result1.Should().Be(result2);
    }

    [Theory]
    [InlineData("user-123", "user-456", "POST", "POST", "/api/tokens", "/api/tokens", "test-key-123", "test-key-123", "Different partitions")]
    [InlineData("user-123", "user-123", "POST", "PUT", "/api/tokens", "/api/tokens", "test-key-123", "test-key-123", "Different methods")]
    [InlineData("user-123", "user-123", "POST", "POST", "/api/tokens", "/api/payments", "test-key-123", "test-key-123", "Different paths")]
    [InlineData("user-123", "user-123", "POST", "POST", "/api/tokens", "/api/tokens", "test-key-123", "test-key-456", "Different idempotency keys")]
    public void Hash_WithDifferentInputs_ShouldReturnDifferentHashes(
        string partition1, string partition2, 
        string method1, string method2, 
        string path1, string path2, 
        string key1, string key2, 
        string description)
    {
        // Act
        var result1 = _hasher.Hash(partition1, method1, new PathString(path1), key1);
        var result2 = _hasher.Hash(partition2, method2, new PathString(path2), key2);

        // Assert
        result1.Should().NotBe(result2, description);
    }

    [Fact]
    public void Hash_ShouldUseCorrectInputFormat()
    {
        // Arrange
        const string partition = "user-123";
        const string method = "POST";
        var path = new PathString("/api/tokens");
        const string idempotencyKey = "test-key-123";

        // Act
        var result = _hasher.Hash(partition, method, path, idempotencyKey);

        // Assert
        // Verify the hash is generated from the expected input format: "partition\method\path\idempotencyKey"
        var expectedInput = $"{partition}\n{method}\n{path}\n{idempotencyKey}";
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedInput));
        var expectedResult = $"idem:{Convert.ToHexString(expectedHash)}";
        
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void Hash_WithLongInputs_ShouldReturnValidHash()
    {
        // Arrange
        var partition = new string('a', 1000);
        const string method = "POST";
        var path = new PathString("/" + new string('b', 1000));
        var idempotencyKey = new string('c', 1000);

        // Act
        var result = _hasher.Hash(partition, method, path, idempotencyKey);

        // Assert
        result.Should().StartWith("idem:");
        result.Should().HaveLength(69);
        result.Should().MatchRegex(@"^idem:[A-Fa-f0-9]{64}$");
    }

    [Fact]
    public void Hash_ShouldBeCaseSensitive()
    {
        // Arrange
        const string partition = "User-123";
        const string method = "post";
        var path = new PathString("/API/TOKENS");
        const string idempotencyKey = "Test-Key-123";

        // Act
        var result1 = _hasher.Hash(partition, method, path, idempotencyKey);
        var result2 = _hasher.Hash(partition.ToLower(), method.ToUpper(), path, idempotencyKey);

        // Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void Hash_WithNullInputs_ShouldHandleGracefully()
    {
        // Arrange
        string? partition = null;
        string? method = null;
        var path = new PathString();
        string? idempotencyKey = null;

        // Act
        var result = _hasher.Hash(partition!, method!, path, idempotencyKey!);

        // Assert
        result.Should().StartWith("idem:");
        result.Should().HaveLength(69);
        result.Should().MatchRegex("^idem:[A-Fa-f0-9]{64}$");
    }

    [Fact]
    public void Hash_ShouldImplementIIdempotencyKeyHasher()
    {
        // Assert
        _hasher.Should().BeAssignableTo<IIdempotencyKeyHasher>();
    }

    [Fact]
    public void Hash_ShouldBeDeterministic()
    {
        // Arrange
        const string partition = "user-123";
        const string method = "POST";
        var path = new PathString("/api/tokens");
        const string idempotencyKey = "test-key-123";

        // Act - Generate multiple hashes
        var results = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            results.Add(_hasher.Hash(partition, method, path, idempotencyKey));
        }

        // Assert - All results should be identical
        results.Should().AllBeEquivalentTo(results.First());
    }
}