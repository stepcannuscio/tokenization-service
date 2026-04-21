using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using FluentAssertions;
using Tokenization.Infrastructure.Crypto.KeyVault.Mapping;
using Tokenization.Tests.Shared.Utils.KeyVault;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Crypto.KeyVault.Mapping;

public class KeyVaultMappingExtensionsTests
{
    [Fact]
    public void ToKeyVersionInfo_Works()
    {
        var key = TestKeyVaultKey.New("https://vault/keys/pay-kek/v1", "v1", DateTimeOffset.UtcNow);
        var info = key.ToKeyVersionInfo(isCurrentKey: true);
        info.Should().NotBeNull();
        info.IsCurrent.Should().BeTrue();
    }

    [Fact]
    public void ToKeyWrapPayload_Works()
    {
        const string keyId = "https://vault/keys/pay-kek/v1";
        var encrypted = new byte[] { 1, 2, 3 };
        var algo = KeyWrapAlgorithm.A256KW;

        // Use built-in mock for CryptographyClient
        var result = CryptographyModelFactory.WrapResult(keyId, encrypted, algo);

        var payload = result.ToKeyWrapPayload();
        payload.Should().NotBeNull();
    }
}
