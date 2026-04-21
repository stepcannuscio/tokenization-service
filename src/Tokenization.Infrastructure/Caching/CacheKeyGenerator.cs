using System.Text;
using System.Text.RegularExpressions;

namespace Tokenization.Infrastructure.Caching;

/// <summary>
/// Safe cache key generator that provides proper namespacing, validation, and collision prevention.
/// </summary>
/// <remarks>
/// <para>This implementation ensures:</para>
/// <list type="bullet">
///     <item>Proper namespacing to prevent key collisions</item>
///     <item>Input validation and sanitization</item>
///     <item>Consistent key format across the application</item>
///     <item>Support for versioned and hierarchical keys</item>
///     <item>Protection against cache key injection attacks</item>
/// </list>
/// </remarks>
internal sealed partial class CacheKeyGenerator : ICacheKeyGenerator
{
    private const int MaxKeyLength = 250; // Conservative limit for most cache systems
    private const int MaxNamespaceLength = 50;
    private const int MaxKeyPartLength = 100;
    private const char KeySeparator = '/';
    private const char VersionSeparator = ':';
    
    // Regex pattern for safe cache key characters: alphanumeric, hyphens, underscores, dots, colons, and forward slashes
    // This pattern allows: a-z, A-Z, 0-9, -, _, ., :, /
    private static readonly Regex SafeCharacterPattern = SafeCharacterRegex();

    /// <inheritdoc />
    public string GenerateKey(string @namespace, params string[] keyParts)
    {
        ValidateNamespace(@namespace);
        ValidateKeyParts(keyParts);

        var keyBuilder = new StringBuilder();
        
        // Add namespace
        keyBuilder.Append(@namespace);
        
        // Add key parts
        foreach (var part in keyParts)
        {
            keyBuilder.Append(KeySeparator);
            keyBuilder.Append(SanitizeForCacheKey(part));
        }

        var result = keyBuilder.ToString();
        ValidateFinalKey(result);
        
        return result;
    }

    /// <inheritdoc />
    public string GenerateVersionedKey(string @namespace, string version, params string[] keyParts)
    {
        ValidateNamespace(@namespace);
        ValidateKeyPart(version, nameof(version));
        ValidateKeyParts(keyParts);

        var keyBuilder = new StringBuilder();
        
        // Add namespace
        keyBuilder.Append(@namespace);
        
        // Add version
        keyBuilder.Append(VersionSeparator);
        keyBuilder.Append(SanitizeForCacheKey(version));
        
        // Add key parts
        foreach (var part in keyParts)
        {
            keyBuilder.Append(KeySeparator);
            keyBuilder.Append(SanitizeForCacheKey(part));
        }

        var result = keyBuilder.ToString();
        ValidateFinalKey(result);
        
        return result;
    }

    /// <inheritdoc />
    public bool IsValidCacheKey(string cacheKey)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            return false;

        if (cacheKey.Length > MaxKeyLength)
            return false;

        // Check for unsafe characters using regex
        return SafeCharacterPattern.IsMatch(cacheKey);
    }

    /// <inheritdoc />
    public string SanitizeForCacheKey(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "empty";

        var sanitized = new StringBuilder();
        
        foreach (var character in input)
        {
            // Check if character matches the safe pattern (single character)
            if (SafeCharacterPattern.IsMatch(character.ToString()))
            {
                sanitized.Append(character);
            }
            else if (char.IsWhiteSpace(character))
            {
                sanitized.Append('_');
            }
            else
            {
                // Convert unsafe characters to their hex representation
                var bytes = Encoding.UTF8.GetBytes(character.ToString());
                foreach (var b in bytes)
                {
                    sanitized.Append('x');
                    sanitized.Append(b.ToString("X2"));
                }
            }
        }

        var result = sanitized.ToString();
        
        // Truncate if too long
        if (result.Length > MaxKeyPartLength)
        {
            result = result[..MaxKeyPartLength];
        }

        // Ensure it's not empty after sanitization
        if (string.IsNullOrWhiteSpace(result))
            result = "sanitized";

        return result;
    }

    private static void ValidateNamespace(string @namespace)
    {
        if (string.IsNullOrWhiteSpace(@namespace))
            throw new ArgumentException("Namespace cannot be null or empty", nameof(@namespace));

        if (@namespace.Length > MaxNamespaceLength)
            throw new ArgumentException($"Namespace cannot exceed {MaxNamespaceLength} characters", nameof(@namespace));

        if (!IsValidNamespace(@namespace))
            throw new ArgumentException("Namespace contains invalid characters", nameof(@namespace));
    }

    private static void ValidateKeyParts(string[] keyParts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyParts.ToString());

        foreach (var part in keyParts)
        {
            ValidateKeyPart(part, nameof(keyParts));
        }
    }

    private static void ValidateKeyPart(string keyPart, string paramName)
    {
        if (string.IsNullOrWhiteSpace(keyPart))
            throw new ArgumentException("Key part cannot be null or empty", paramName);

        if (keyPart.Length > MaxKeyPartLength)
            throw new ArgumentException($"Key part cannot exceed {MaxKeyPartLength} characters", paramName);
    }

    private void ValidateFinalKey(string cacheKey)
    {
        if (!IsValidCacheKey(cacheKey))
            throw new ArgumentException($"Generated cache key is invalid: {cacheKey}");
    }

    private static bool IsValidNamespace(string @namespace)
    {
        // Namespace should start with a letter and contain only safe characters
        return char.IsLetter(@namespace[0]) && SafeCharacterPattern.IsMatch(@namespace);
    }

    [GeneratedRegex(@"^[a-zA-Z0-9\-_\.:/]+$", RegexOptions.Compiled)]
    private static partial Regex SafeCharacterRegex();
}