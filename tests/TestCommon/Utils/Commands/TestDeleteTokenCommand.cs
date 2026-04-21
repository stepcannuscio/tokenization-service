using Tokenization.Application.Handlers.DeleteToken;

namespace Tokenization.Tests.Shared.Utils.Commands;

internal static class TestDeleteTokenCommand
{
    public static DeleteTokenCommand Valid()
    {
        return new DeleteTokenCommand
        {
            Token = "token-123"
        };
    }
}
