using System.ComponentModel.DataAnnotations;

namespace Tokenization.Api.Requests.v1;

/// <summary>
/// Represents a request to detokenize a token.
/// </summary>
public sealed record DetokenizeTokenRequest
{
    /// <summary>
    /// The token to detokenize.
    /// </summary>
    [Required]
    public required string Token { get; init; }
}
