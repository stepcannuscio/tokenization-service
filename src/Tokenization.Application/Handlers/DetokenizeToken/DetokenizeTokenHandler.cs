using MediatR;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.Exceptions;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Application.Handlers.DetokenizeToken;

/// <summary>
/// Redeems and detokenizes a token, enforcing limits and updating last-used timestamps.
/// </summary>
internal sealed class DetokenizeTokenHandler(ITokenService service)
    : IRequestHandler<DetokenizeTokenCommand, DetokenizedToken>
{
    /// <summary>Handles the <see cref="DetokenizeTokenCommand"/>.</summary>
    /// <param name="cmd"><see cref="DetokenizeTokenCommand"/> to handle.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A sensitive <see cref="DetokenizedToken"/>.</returns>
    /// <exception cref="TokenNotFoundException">Thrown if the token does not exist.</exception>
    /// <exception cref="TokenInactiveException">Thrown if the token is inactive and cannot be used.</exception>
    /// <exception cref="TokenExpiredException">Thrown if the token is expired.</exception>
    /// <exception cref="TokenUsageExceededException">Thrown if the token exceeds its allowed number of uses during redemption.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the <c>IEncryptionService</c> fails to decrypt the token.</exception>
    public async Task<DetokenizedToken> Handle(DetokenizeTokenCommand cmd, CancellationToken ct)
    {
        await service.RedeemTokenAsync(cmd.Token, DateTimeOffset.Now, ct);
        return await service.DetokenizeTokenAsync(cmd.Token, ct);
    }
}