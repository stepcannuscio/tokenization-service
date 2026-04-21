using MediatR;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Application.Handlers.GetToken;

/// <summary>
/// Command to retrieve token information by token ID.
/// </summary>
internal sealed class GetTokenCommand : IRequest<TokenSummary>
{
    /// <summary>
    /// The token identifier to retrieve.
    /// </summary>
    public string Token { get; set; } = null!;
}