using FluentValidation.TestHelper;
using Tokenization.Application.Handlers.CreateToken;
using Tokenization.Domain.ValueObjects;
using Tokenization.Tests.Shared.Utils.Commands;
using Xunit;

namespace Tokenization.Tests.Unit.Application.Handlers.CreateToken;

public class CreateTokenValidatorTests
{
    private readonly CreateTokenValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.TestValidate(TestCreateTokenCommand.Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invalid_Pan_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.Card ??= new CardPlaintext();
        cmd.Card.Pan = "4111x";
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Card!.Pan);
    }

    [Fact]
    public void Invalid_ExpMonth_Range_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.Card ??= new CardPlaintext();

        cmd.Card.ExpMonth = 0;
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Card!.ExpMonth);


        cmd.Card.ExpMonth = 13;
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Card!.ExpMonth);
    }

    [Fact]
    public void Invalid_ExpYear_TooLow_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.Card ??= new CardPlaintext();
        cmd.Card.ExpYear = DateTime.UtcNow.Year - 1;
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Card!.ExpYear);
    }

    [Fact]
    public void Invalid_TenantId_Empty_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.TenantId = string.Empty;
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Invalid_CustomerId_Empty_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.CustomerId = string.Empty;
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void Invalid_PaymentMethodType_Empty_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.PaymentMethodType = string.Empty;
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethodType);
    }

    [Fact]
    public void Invalid_PaymentMethodType_NotEnumName_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.PaymentMethodType = "MadeUp";
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethodType);
    }

    [Fact]
    public void Invalid_TokenTypeType_Empty_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.TokenType = string.Empty;
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.TokenType);
    }

    [Fact]
    public void Invalid_TokenType_NotEnumName_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.TokenType = "MadeUp";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.TokenType);
    }

    [Fact]
    public void Invalid_StoredCredentialInitiator_NotEnumName_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.StoredCredentialInitiator = "MadeUp";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.StoredCredentialInitiator);
    }

    [Fact]
    public void Invalid_StoredCredentialReason_NotEnumName_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.StoredCredentialReason = "MadeUp";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.StoredCredentialReason);
    }

    [Fact]
    public void Invalid_Currency_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.Currency = "US";
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void Invalid_Country_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.Country = "USA";
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Country);
    }

    [Fact]
    public void Invalid_ExpiresAt_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public void Invalid_MaxUses_Fails()
    {
        var cmd = TestCreateTokenCommand.Valid();
        cmd.MaxUses = -1;
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MaxUses);
    }
}
