using FluentAssertions;
using Tokenization.Infrastructure.Db.Mapping.TokenRecord;
using Tokenization.Tests.Shared.Utils.ValueObjects;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Db.Mapping.TokenRecord;

public class TokenRecordSummaryMapperTests
{
    [Fact]
    public void Projects_NonSensitive_Fields()
    {
        var args = TestCreateTokenArgs.Valid();
        var env = TestEncryptedPayload.Valid();

        var entity = args.ToTokenRecord(env);

        var summary = entity.ToSummary();

        summary.Token.Should().Be(entity.Token);
        summary.MaskedData.Should().Be(entity.MaskedData);
        summary.Last4.Should().Be(entity.Last4);
        summary.PaymentMethodType.Should().Be(entity.PaymentMethodType);
        summary.Network.Should().Be(entity.Network);
        summary.Currency.Should().Be(entity.Currency);
        summary.Country.Should().Be(entity.Country);
        summary.TenantId.Should().Be(entity.TenantId);
        summary.CustomerId.Should().Be(entity.CustomerId);
        summary.TokenType.Should().Be(entity.TokenType);
        summary.MaxUses.Should().Be(entity.MaxUses);
    }
}