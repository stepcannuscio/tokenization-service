using MediatR;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.Exceptions;

namespace Tokenization.Application.Handlers.DeleteToken;

/// <summary>
/// Handles the deleting of a token by delegating the request to the token service.
/// </summary>
internal sealed class DeleteTokenHandler(ITokenService service) : IRequestHandler<DeleteTokenCommand, bool>
{
    /// <summary>Handles the <see cref="DeleteTokenCommand"/>.</summary>
    /// <param name="cmd"><see cref="DeleteTokenCommand"/> to handle.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A boolean value indicating if the token deactivation was successful (always true if successful).</returns>
    /// <exception cref="TokenNotFoundException">Thrown when the token does not exist.</exception>
    /// <exception cref="TenantAccessDeniedException">Thrown when the caller does not have sufficient access to delete the token.</exception>
    public async Task<bool> Handle(DeleteTokenCommand cmd, CancellationToken ct)
    {
        await service.DeleteTokenAsync(cmd.Token, ct);
        return true;
    }
}
