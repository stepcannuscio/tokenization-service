using System.ComponentModel.DataAnnotations;

namespace Tokenization.Api.Requests.v1;

/// <summary>
/// Request to retrieve token information.
/// </summary>
public sealed record GetTokenRequest
{
    /// <summary>
    /// The token identifier to retrieve.
    /// </summary>
    [Required]
    public required string Token { get; init; }
}
