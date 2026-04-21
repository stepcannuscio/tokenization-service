using FluentAssertions;
using Tokenization.Infrastructure.Crypto.KeyVault.Mapping;
using Tokenization.Tests.Shared.Utils.KeyVault;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Crypto.KeyVault.Mapping;

public class KeyVaultKeyVersionInfoMapperTests
{
    [Fact]
    public void Map_Throws_On_Null_Source()
    {
        var mapper = new KeyVaultKeyVersionInfoMapper();
        Action act = () => mapper.Map(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_Maps_All_Properties()
    {
        var key = TestKeyVaultKey.New("https://vault/keys/pay-kek/v1", "v1", DateTimeOffset.UtcNow);
        var mapper = new KeyVaultKeyVersionInfoMapper();

        var mapped = mapper.Map(key, isCurrentKey: true);

        mapped.KekKeyId.Should().BeEquivalentTo(key.Id.ToString());
        mapped.IsCurrent.Should().BeTrue();
        mapped.CreatedAt.Should().Be(key.Properties.CreatedOn);
    }

    [Fact]
    public void Map_Produces_KeyVersionInfo_With_Valid_Current_Property()
    {
        var key = TestKeyVaultKey.New("https://vault/keys/pay-kek/v1", "v1", DateTimeOffset.UtcNow);
        var mapper = new KeyVaultKeyVersionInfoMapper();

        var versionInfo1 = mapper.Map(key, isCurrentKey: true);
        var versionInfo2 = mapper.Map(key, isCurrentKey: false);
        var versionInfo3 = mapper.Map(key);

        versionInfo1.IsCurrent.Should().BeTrue();
        versionInfo2.IsCurrent.Should().BeFalse();
        versionInfo3.IsCurrent.Should().BeFalse();
    }
}