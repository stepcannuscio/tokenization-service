using FluentAssertions;
using Tokenization.Infrastructure.Db.Mapping.TokenRecord;
using Tokenization.Tests.Shared.Utils.ValueObjects;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Db.Mapping.TokenRecord;

public class CreateTokenArgsToTokenRecordMapperTests
{
    [Fact]
    public void Maps_All_Fields_And_Generates_Token_When_Null()
    {
        var args = TestCreateTokenArgs.Valid();
        var env = TestEncryptedPayload.Valid();

        var entity = args.ToTokenRecord(env);

        entity.Token.Should().NotBeNullOrWhiteSpace();
        entity.MaskedData.Should().Be(args.MaskedData);
        entity.Last4.Should().Be(args.Last4);
        entity.PaymentMethodType.Should().Be(args.PaymentMethodType);
        entity.Network.Should().Be(args.Network);
        entity.PaymentMethodMetadata.Should().Be(args.PaymentMethodMetadata);
        entity.Currency.Should().Be(args.Currency);
        entity.Country.Should().Be(args.Country);
        entity.TenantId.Should().Be(args.TenantId);
        entity.CustomerId.Should().Be(args.CustomerId);
        entity.TokenType.Should().Be(args.TokenType);
        entity.InitialTransactionId.Should().NotBeNullOrWhiteSpace();
        entity.MaxUses.Should().Be(args.MaxUses);
        entity.IsActive.Should().BeTrue();
        entity.UsageCount.Should().Be(0);
        entity.EncryptedPayload.Should().NotBeNull();
    }

    [Fact]
    public void Uses_TokenOverride_When_Provided()
    {
        var args = TestCreateTokenArgs.Valid("caller-token");
        var env = TestEncryptedPayload.Valid();

        var entity = args.ToTokenRecord(env, tokenOverride: "override");

        entity.Token.Should().Be("override");
    }
}