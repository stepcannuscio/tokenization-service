using Tokenization.Domain.ValueObjects;

namespace Tokenization.Domain.Abstractions;

/// <summary>
/// Represents a typed wrapper around a concrete key-management client and the metadata for the
/// specific key version that the client instance is intended to operate against.
/// </summary>
/// <typeparam name="T">
/// The underlying client type (for example, a cloud KMS or Key Vault client) that will be used to
/// perform key operations such as wrap/unwrap.
/// </typeparam>
/// <remarks>
/// Implementations typically pair the <see cref="Client"/> with a <see cref="VersionInfo"/> to express
/// which logical key (and version) the client is bound to. Instances are commonly cached by key id/version.
/// Property setters should not perform I/O; initialization and network calls should happen outside this type.
/// </remarks>
internal interface IKeyClient<T>
{
    /// <summary>
    /// Gets or sets the underlying concrete client used to talk to the key store.
    /// Implementations should ensure the instance is safe to use for the intended lifetime (e.g., thread-safe if shared).
    /// </summary>
    T Client { get; set; }

    /// <summary>
    /// Gets or sets the version metadata describing the logical key.
    /// </summary>
    KeyVersionInfo VersionInfo { get; set; }
}
