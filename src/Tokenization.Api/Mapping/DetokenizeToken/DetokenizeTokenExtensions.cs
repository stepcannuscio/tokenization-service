using Tokenization.Api.Requests.v1;
using Tokenization.Api.Responses;
using Tokenization.Application.Handlers.DetokenizeToken;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Api.Mapping.DetokenizeToken;

/// <summary>
/// Provides extension methods for converting between API request/response objects and application layer commands/DTOs.
/// </summary>
internal static class DetokenizeTokenExtensions
{
    /// <summary>
    /// Converts a DetokenizeTokenRequest to a DetokenizeTokenCommand using the DetokenizeTokenMapper.
    /// </summary>
    /// <param name="request">The DetokenizeTokenRequest to convert.</param>
    /// <returns>A DetokenizeTokenCommand containing the mapped data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request parameter is null.</exception>
    public static DetokenizeTokenCommand ToDetokenizeTokenCommand(
        this DetokenizeTokenRequest request)
    {
        return new DetokenizeTokenMapper().MapRequest(request);
    }

    /// <summary>
    /// Converts a DetokenizedToken to a DetokenizeTokenResponse using the DetokenizeTokenMapper.
    /// </summary>
    /// <param name="result">The detokenized token data.</param>
    /// <returns>A DetokenizeTokenResponse containing the mapped data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the dto parameter is null.</exception>
    public static DetokenizeTokenResponse ToDetokenizeTokenResponse(
        this DetokenizedToken result)
    {
        return new DetokenizeTokenMapper().MapResponse(result);
    }
}