using FluentValidation.TestHelper;
using Tokenization.Application.Handlers.DeleteToken;
using Tokenization.Tests.Shared.Utils.Commands;
using Xunit;

namespace Tokenization.Tests.Unit.Application.Handlers.DeleteToken;

public class DeleteTokenValidatorTests
{
    private readonly DeleteTokenValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.TestValidate(TestDeleteTokenCommand.Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invalid_Token_Empty_Fails()
    {
        var cmd = TestDeleteTokenCommand.Valid();
        cmd.Token = string.Empty;
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }
}
