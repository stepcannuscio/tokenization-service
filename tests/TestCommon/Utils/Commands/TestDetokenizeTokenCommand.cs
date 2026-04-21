using Tokenization.Application.Handlers.DetokenizeToken;

namespace Tokenization.Tests.Shared.Utils.Commands;

internal static class TestDetokenizeTokenCommand
{
    public static DetokenizeTokenCommand Valid()
    {
        return new DetokenizeTokenCommand
        {
            Token = "token-123"
        };
    }
}