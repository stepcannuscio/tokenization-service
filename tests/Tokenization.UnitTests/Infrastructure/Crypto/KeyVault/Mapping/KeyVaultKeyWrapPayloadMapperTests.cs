using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using FluentAssertions;
using Tokenization.Infrastructure.Crypto.KeyVault.Mapping;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Crypto.KeyVault.Mapping;

public class KeyVaultKeyWrapPayloadMapperTests
{
    [Fact]
    public void Map_Throws_On_Null_Source()
    {
        var mapper = new KeyVaultKeyWrapPayloadMapper();
        Action act = () => mapper.Map(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_Maps_All_Properties()
    {
        const string keyId = "https://vault/keys/pay-kek/v1";
        var encrypted = new byte[] { 1, 2, 3 };
        var algo = KeyWrapAlgorithm.A256KW;
        var mapper = new KeyVaultKeyWrapPayloadMapper();

        // Use built-in mock for CryptographyClient
        var result = CryptographyModelFactory.WrapResult(keyId, encrypted, algo);

        var payload = mapper.Map(result);

        payload.WrappedDek.Should().BeEquivalentTo(encrypted);
        payload.KekKeyId.Should().Be(keyId);
        payload.Algorithm.Should().Be(algo.ToString());
        payload.WrappedAt.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(2));
    }
}