using FluentAssertions;
using Tokenization.Infrastructure.Crypto.InMemory;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Crypto.InMemory;

public class InMemoryKeyClientTests
{
    [Fact]
    public void Ctor_Sets_Client_To_32_Bytes()
    {
        var sut = new InMemoryKeyClient("alpha", 1, true);

        sut.Client.Should().HaveCount(32);
    }

    [Fact]
    public void Ctor_Sets_KekKeyId_With_ZeroPadded_Version()
    {
        var s1 = new InMemoryKeyClient("alpha", 1, true);
        var s2 = new InMemoryKeyClient("beta", 42, false);

        s1.VersionInfo.KekKeyId.Should().Be("inmemory://keys/alpha/v0001");
        s2.VersionInfo.KekKeyId.Should().Be("inmemory://keys/beta/v0042");
    }

    [Fact]
    public void Ctor_Sets_IsCurrent_Flag()
    {
        var current = new InMemoryKeyClient("alpha", 2, true);
        var notCurrent = new InMemoryKeyClient("alpha", 3, false);

        current.VersionInfo.IsCurrent.Should().BeTrue();
        notCurrent.VersionInfo.IsCurrent.Should().BeFalse();
    }

    [Fact]
    public void Ctor_Sets_CreatedAt_Close_To_UtcNow()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-5);

        var sut = new InMemoryKeyClient("time-test", 7, true);

        var after = DateTimeOffset.UtcNow.AddSeconds(5);
        sut.VersionInfo.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        sut.VersionInfo.CreatedAt.Offset.Should().Be(TimeSpan.Zero); // UTC
    }

    [Fact]
    public void KekKeyId_Format_Allows_Large_Version_Numbers()
    {
        var sut = new InMemoryKeyClient("gamma", 12345, true);

        // "0000" ensures a minimum of 4 digits; larger values are unpadded.
        sut.VersionInfo.KekKeyId.Should().Be("inmemory://keys/gamma/v12345");
    }
}