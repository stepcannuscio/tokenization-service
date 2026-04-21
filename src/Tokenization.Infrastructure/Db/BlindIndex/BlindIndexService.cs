using System.Text;
using Tokenization.Domain.Abstractions;

namespace Tokenization.Infrastructure.Db.BlindIndex;

/// <summary>
/// Default blind-index implementation using HMAC-SHA256 over UTF-8 text.
/// </summary>
internal sealed class BlindIndexService(IKeyProvider keyProvider, string keyName) : IBlindIndexService
{
    private const int HashLength = 32;
    
    /// <inheritdoc />
    public async Task<byte[]> ComputeAsync(string value, string? keyId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

        var data = Encoding.UTF8.GetBytes(value);
        var hash = await keyProvider.SignDataAsync(data, keyName, keyId, ct);
        
        // Ensure we have exactly 32 bytes
        if (hash.Length == HashLength)
            return hash;

        var result = new byte[32];
        Array.Copy(hash, result, Math.Min(hash.Length, 32));
        return result;
    }
}