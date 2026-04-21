using Tokenization.Domain.ValueObjects;

namespace Tokenization.Infrastructure.Crypto.Mapping;

/// <summary>
/// Maps an external key model (from a KMS/Vault SDK) to a domain <see cref="KeyVersionInfo"/>.
/// </summary>
/// <typeparam name="TSource">The external key model type (e.g., provider-specific key metadata).</typeparam>
/// <remarks>
/// The resulting <see cref="KeyVersionInfo"/> should preserve a stable key identifier (key ID + version),
/// creation/activation timestamps when available, and whether the version is the current one.
/// </remarks>
internal interface IKeyVersionInfoMapper<in TSource>
{
    /// <summary>
    /// Projects external key metadata into a domain <see cref="KeyVersionInfo"/>.
    /// </summary>
    /// <param name="source">The external key object to map. Must not be <c>null</c>.</param>
    /// <param name="isCurrentKey">Whether the mapped version should be flagged as current.</param>
    /// <returns>A populated <see cref="KeyVersionInfo"/> for the given key version.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <c>null</c>.</exception>
    KeyVersionInfo Map(TSource source, bool isCurrentKey = false);
}
