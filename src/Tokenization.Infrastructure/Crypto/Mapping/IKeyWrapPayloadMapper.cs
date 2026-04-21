using Tokenization.Domain.ValueObjects;

namespace Tokenization.Infrastructure.Crypto.Mapping;

/// <summary>
/// Maps the result of a provider wrap operation (e.g., a KMS/Vault wrap response)
/// to a durable, store-able <see cref="KeyWrapPayload"/>.
/// </summary>
/// <typeparam name="TSource">The provider-specific wrap result type (e.g., <c>WrapResult</c>).</typeparam>
/// <remarks>
/// Implementations should include the wrapped DEK bytes, the wrapping algorithm name,
/// the KEK identifier, and a timestamp. Sensitive key material (e.g., plaintext KEK/DEK)
/// must never be included in the mapped payload.
/// </remarks>
internal interface IKeyWrapPayloadMapper<in TSource>
{
    /// <summary>
    /// Converts a provider wrap result into a <see cref="KeyWrapPayload"/> for persistence/transport.
    /// </summary>
    /// <param name="source">The provider wrap result to map. Must not be <c>null</c>.</param>
    /// <returns>The mapped <see cref="KeyWrapPayload"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <c>null</c>.</exception>
    KeyWrapPayload Map(TSource source);
}
