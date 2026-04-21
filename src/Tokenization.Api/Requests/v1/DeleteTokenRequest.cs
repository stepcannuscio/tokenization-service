using System.ComponentModel.DataAnnotations;

namespace Tokenization.Api.Requests.v1;

/// <summary>
/// Represents a request to delete a token.
/// </summary>
public sealed record DeleteTokenRequest
{
    /// <summary>
    /// The token to delete.
    /// </summary>
    [Required]
    public required string Token { get; init; }
}
