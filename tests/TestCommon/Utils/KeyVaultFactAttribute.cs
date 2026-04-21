using Xunit;

namespace Tokenization.Tests.Shared.Utils;

/// <summary>
/// Marks Azure Key Vault integration tests as opt-in.
/// </summary>
internal sealed class KeyVaultFactAttribute : FactAttribute
{
    public KeyVaultFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("RUN_KEYVAULT_TESTS"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set RUN_KEYVAULT_TESTS=true and provide Key Vault configuration to run this test.";
        }
    }
}
