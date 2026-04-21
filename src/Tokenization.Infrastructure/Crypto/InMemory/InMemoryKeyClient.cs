using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Infrastructure.Crypto.InMemory;

/// <inheritdoc />
/// <summary>
/// In-memory implementation of <see cref="IKeyClient{T}"/> where the underlying client is the raw KEK bytes.
/// Intended for development and tests; KEK material is generated locally and never persisted.
/// </summary>
internal sealed class InMemoryKeyClient(string keyName, int version, bool isCurrent) : IKeyClient<byte[]>
{
    /// <summary>
    /// Initializes a new client for cache deserialization. Do not use directly in application code.
    /// </summary>
    [JsonConstructor]
    public InMemoryKeyClient() : this("json-deserialize", 0, false) { }

    /// <summary>
    /// The KEK bytes (e.g., a 256-bit AES key) used to wrap/unwrap DEKs.
    /// </summary>
    public byte[] Client { get; set; } = RandomNumberGenerator.GetBytes(32);

    /// <inheritdoc />
    public KeyVersionInfo VersionInfo { get; set; } = new()
    {
        KekKeyId = $"inmemory://keys/{keyName}/v{version:0000}",
        CreatedAt = DateTimeOffset.UtcNow,
        IsCurrent = isCurrent
    };
}
