using MediatR;

namespace Tokenization.Application.Handlers.DeleteToken;

/// <summary>
/// Command to delete an existing token.
/// </summary>
internal sealed class DeleteTokenCommand : IRequest<bool>
{
    /// <summary>The token to delete.</summary>
    public string Token { get; set; } = null!;
}
