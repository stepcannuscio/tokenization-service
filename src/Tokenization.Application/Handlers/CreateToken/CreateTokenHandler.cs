using MediatR;
using Tokenization.Domain.Exceptions;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Application.Handlers.CreateToken;

/// <summary>
/// Securely handles sensitive PCI card data by issuing a token corresponding to the encrypted card data. 
/// </summary>
internal sealed class CreateTokenHandler(
    ITokenService service) : IRequestHandler<CreateTokenCommand, TokenSummary>
{
    /// <summary>Handles the <see cref="CreateTokenCommand"/>.</summary>
    /// <param name="cmd"><see cref="CreateTokenCommand"/> to handle.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A minimal <see cref="TokenSummary"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="ITokenService.IssueTokenAsync"/> fails.</exception>
    /// <exception cref="TenantAccessDeniedException">Thrown when the caller does not have sufficient access to issue the token.</exception>
    public async Task<TokenSummary> Handle(CreateTokenCommand cmd, CancellationToken ct)
    {
        var createTokenArgs = cmd.ToCreateTokenArgs();
        var sensitivePayload = cmd.ToSensitivePayload();

        return await service.IssueTokenAsync(createTokenArgs, sensitivePayload, ct);
    }
}
