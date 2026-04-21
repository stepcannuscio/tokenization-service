using FluentAssertions;
using Tokenization.Infrastructure.Caching;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Caching;

public class CacheKeyGeneratorTests
{
    private readonly CacheKeyGenerator _generator = new();

    [Fact]
    public void GenerateKey_WithValidInputs_ReturnsValidCacheKey()
    {
        // Act
        var result = _generator.GenerateKey("TestNamespace", "key1", "key2");

        // Assert
        result.Should().Be("TestNamespace/key1/key2");
        _generator.IsValidCacheKey(result).Should().BeTrue();
    }

    [Fact]
    public void GenerateVersionedKey_WithValidInputs_ReturnsValidCacheKey()
    {
        // Act
        var result = _generator.GenerateVersionedKey("TestNamespace", "v1", "key1");

        // Assert
        result.Should().Be("TestNamespace:v1/key1");
        _generator.IsValidCacheKey(result).Should().BeTrue();
    }

    [Theory]
    [InlineData("", "key1")]
    [InlineData(null, "key1")]
    [InlineData("   ", "key1")]
    public void GenerateKey_WithInvalidNamespace_ThrowsArgumentException(string? @namespace, string keyPart)
    {
        // Act & Assert
        var act = () => _generator.GenerateKey(@namespace!, keyPart);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("TestNamespace", "", "key2")]
    [InlineData("TestNamespace", null, "key2")]
    [InlineData("TestNamespace", "   ", "key2")]
    public void GenerateKey_WithInvalidKeyParts_ThrowsArgumentException(string @namespace, string? keyPart1, string keyPart2)
    {
        // Act & Assert
        var act = () => _generator.GenerateKey(@namespace, keyPart1!, keyPart2);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateKey_WithTooLongNamespace_ThrowsArgumentException()
    {
        // Arrange
        var longNamespace = new string('a', 51); // Exceeds MaxNamespaceLength

        // Act & Assert
        var act = () => _generator.GenerateKey(longNamespace, "key1");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateKey_WithTooLongKeyPart_ThrowsArgumentException()
    {
        // Arrange
        var longKeyPart = new string('a', 101); // Exceeds MaxKeyPartLength

        // Act & Assert
        var act = () => _generator.GenerateKey("TestNamespace", longKeyPart);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("ValidKey")]
    [InlineData("valid-key")]
    [InlineData("valid_key")]
    [InlineData("valid.key")]
    [InlineData("ValidKey123")]
    [InlineData("a")]
    public void IsValidCacheKey_WithValidKeys_ReturnsTrue(string cacheKey)
    {
        // Act & Assert
        _generator.IsValidCacheKey(cacheKey).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("key with spaces")]
    [InlineData("key@invalid")]
    [InlineData("key#invalid")]
    [InlineData("key$invalid")]
    [InlineData("key%invalid")]
    [InlineData("key^invalid")]
    [InlineData("key&invalid")]
    [InlineData("key*invalid")]
    [InlineData("key(invalid")]
    [InlineData("key)invalid")]
    [InlineData("key+invalid")]
    [InlineData("key=invalid")]
    [InlineData("key[invalid")]
    [InlineData("key]invalid")]
    [InlineData("key{invalid")]
    [InlineData("key}invalid")]
    [InlineData("key|invalid")]
    [InlineData("key\\invalid")]
    [InlineData("key;invalid")]
    [InlineData("key\"invalid")]
    [InlineData("key'invalid")]
    [InlineData("key<invalid")]
    [InlineData("key>invalid")]
    [InlineData("key,invalid")]
    [InlineData("key?invalid")]
    public void IsValidCacheKey_WithInvalidKeys_ReturnsFalse(string? cacheKey)
    {
        // Act & Assert
        _generator.IsValidCacheKey(cacheKey!).Should().BeFalse();
    }

    [Fact]
    public void IsValidCacheKey_WithTooLongKey_ReturnsFalse()
    {
        // Arrange
        var longKey = new string('a', 251); // Exceeds MaxKeyLength

        // Act & Assert
        _generator.IsValidCacheKey(longKey).Should().BeFalse();
    }

    [Theory]
    [InlineData("normal-key", "normal-key")]
    [InlineData("key with spaces", "key_with_spaces")]
    [InlineData("key@invalid", "keyx40invalid")]
    [InlineData("key#invalid", "keyx23invalid")]
    [InlineData("key$invalid", "keyx24invalid")]
    [InlineData("key%invalid", "keyx25invalid")]
    [InlineData("key^invalid", "keyx5Einvalid")]
    [InlineData("key&invalid", "keyx26invalid")]
    [InlineData("key*invalid", "keyx2Ainvalid")]
    [InlineData("key(invalid", "keyx28invalid")]
    [InlineData("key)invalid", "keyx29invalid")]
    [InlineData("key+invalid", "keyx2Binvalid")]
    [InlineData("key=invalid", "keyx3Dinvalid")]
    [InlineData("key[invalid", "keyx5Binvalid")]
    [InlineData("key]invalid", "keyx5Dinvalid")]
    [InlineData("key{invalid", "keyx7Binvalid")]
    [InlineData("key}invalid", "keyx7Dinvalid")]
    [InlineData("key|invalid", "keyx7Cinvalid")]
    [InlineData("key\\invalid", "keyx5Cinvalid")]
    [InlineData("key:invalid", "key:invalid")] // Colon is now allowed
    [InlineData("key;invalid", "keyx3Binvalid")]
    [InlineData("key\"invalid", "keyx22invalid")]
    [InlineData("key'invalid", "keyx27invalid")]
    [InlineData("key<invalid", "keyx3Cinvalid")]
    [InlineData("key>invalid", "keyx3Einvalid")]
    [InlineData("key,invalid", "keyx2Cinvalid")]
    [InlineData("key?invalid", "keyx3Finvalid")]
    [InlineData("key/invalid", "key/invalid")] // Forward slash is now allowed
    [InlineData("", "empty")]
    [InlineData(null, "empty")]
    [InlineData("   ", "empty")]
    public void SanitizeForCacheKey_WithVariousInputs_ReturnsSanitizedKey(string? input, string expected)
    {
        // Act
        var result = _generator.SanitizeForCacheKey(input!);

        // Assert
        result.Should().Be(expected);
        _generator.IsValidCacheKey(result).Should().BeTrue();
    }

    [Fact]
    public void SanitizeForCacheKey_WithTooLongInput_TruncatesToMaxLength()
    {
        // Arrange
        var longInput = new string('a', 150); // Exceeds MaxKeyPartLength

        // Act
        var result = _generator.SanitizeForCacheKey(longInput);

        // Assert
        result.Should().HaveLength(100); // MaxKeyPartLength
        result.Should().Be(new string('a', 100));
        _generator.IsValidCacheKey(result).Should().BeTrue();
    }

    [Fact]
    public void SanitizeForCacheKey_WithUnicodeCharacters_ConvertsToHex()
    {
        // Act
        var result = _generator.SanitizeForCacheKey("key€invalid");

        // Assert
        result.Should().StartWith("key");
        result.Should().EndWith("invalid");
        _generator.IsValidCacheKey(result).Should().BeTrue();
    }

    [Fact]
    public void GenerateKey_WithMultipleKeyParts_JoinsCorrectly()
    {
        // Act
        var result = _generator.GenerateKey("Namespace", "part1", "part2", "part3");

        // Assert
        result.Should().Be("Namespace/part1/part2/part3");
        _generator.IsValidCacheKey(result).Should().BeTrue();
    }

    [Fact]
    public void GenerateVersionedKey_WithMultipleKeyParts_JoinsCorrectly()
    {
        // Act
        var result = _generator.GenerateVersionedKey("Namespace", "v2", "part1", "part2");

        // Assert
        result.Should().Be("Namespace:v2/part1/part2");
        _generator.IsValidCacheKey(result).Should().BeTrue();
    }
}
