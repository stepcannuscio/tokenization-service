using FluentValidation.TestHelper;
using Tokenization.Application.Handlers.DetokenizeToken;
using Tokenization.Tests.Shared.Utils.Commands;
using Xunit;

namespace Tokenization.Tests.Unit.Application.Handlers.DetokenizeToken;

public class DetokenizeTokenValidatorTests
{
    private readonly DetokenizeTokenValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.TestValidate(TestDetokenizeTokenCommand.Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invalid_Token_Empty_Fails()
    {
        var cmd = TestDetokenizeTokenCommand.Valid();
        cmd.Token = string.Empty;
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }
}
