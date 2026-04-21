using MediatR;
using Tokenization.Domain.Exceptions;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Application.Handlers.GetToken;

/// <summary>
/// Handler for retrieving token information by token ID.
/// </summary>
internal sealed class GetTokenHandler(
    ITokenService tokenService) : IRequestHandler<GetTokenCommand, TokenSummary>
{
    /// <summary>
    /// Handles the get token command.
    /// </summary>
    /// <param name="cmd"><see cref="GetTokenCommand"/> to handle..</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A token summary containing non-sensitive token information.</returns>
    /// <exception cref="TokenNotFoundException">Thrown when the token is not found.</exception>
    /// <exception cref="TenantAccessDeniedException">Thrown when the caller doesn't have access to the token.</exception>
    public async Task<TokenSummary> Handle(GetTokenCommand cmd, CancellationToken ct)
    {
        return await tokenService.GetSummaryAsync(cmd.Token, ct);
    }
}
