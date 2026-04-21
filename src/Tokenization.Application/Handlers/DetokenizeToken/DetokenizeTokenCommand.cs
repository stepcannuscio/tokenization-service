using MediatR;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Application.Handlers.DetokenizeToken;

/// <summary>
/// Command to detokenize a token. Must only be called from a PCI-Scope.
/// </summary>
internal sealed class DetokenizeTokenCommand : IRequest<DetokenizedToken>
{
    /// <summary>Token to use.</summary>
    public string Token { get; set; } = null!;
}