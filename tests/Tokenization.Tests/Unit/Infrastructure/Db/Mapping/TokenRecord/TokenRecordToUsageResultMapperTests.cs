using FluentAssertions;
using Tokenization.Infrastructure.Db.Mapping.TokenRecord;
using Tokenization.Tests.Shared.Utils.ValueObjects;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Db.Mapping.TokenRecord;

public class TokenRecordToUsageResultMapperTests
{
    [Fact]
    public void Maps_To_UsageResult()
    {
        var args = TestCreateTokenArgs.Valid("tok-1");
        var env = TestEncryptedPayload.Valid();

        var entity = args.ToTokenRecord(env);
        
        entity.UsageCount = 3;
        entity.IsActive = true;

        var result = entity.ToUsageResult();

        result.Token.Should().Be("tok-1");
        result.UsageCount.Should().Be(3);
        result.IsActive.Should().BeTrue();
    }
}