namespace Tokenization.Infrastructure.Crypto.Enums;

/// <summary>
/// Selects the key-management provider used for storing KEKs and performing wrap/unwrap operations.
/// </summary>
internal enum KeyProviderType
{
    /// <summary>
    /// In-process, development-only provider that generates and stores KEKs in memory.
    /// Useful for local testing where no external KMS/Vault is available.
    /// </summary>
    InMemory,

    /// <summary>
    /// Azure Key Vault provider that delegates wrap/unwrap to the vault.
    /// </summary>
    AzureKeyVault
}