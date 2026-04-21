using Tokenization.Api.Requests.v1;
using Tokenization.Api.Responses;
using Tokenization.Application.Handlers.GetToken;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Api.Mapping.GetToken;

/// <summary>
/// Provides extension methods for converting between API request/response objects and application layer commands/DTOs.
/// </summary>
internal static class GetTokenExtensions
{
    /// <summary>
    /// Converts a GetTokenRequest to a GetTokenCommand using the GetTokenMapper.
    /// </summary>
    /// <param name="request">The GetTokenRequest to convert.</param>
    /// <returns>A GetTokenCommand containing the mapped data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request parameter is null.</exception>
    public static GetTokenCommand ToGetTokenCommand(
        this GetTokenRequest request)
    {
        return new GetTokenMapper().MapRequest(request);
    }

    /// <summary>
    /// Converts a TokenSummary to a GetTokenResponse using the GetTokenMapper.
    /// </summary>
    /// <param name="summary">The non-sensitive token summary.</param>
    /// <returns>A GetTokenResponse containing the mapped data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the dto parameter is null.</exception>
    public static GetTokenResponse ToGetTokenResponse(
        this TokenSummary summary)
    {
        return new GetTokenMapper().MapResponse(summary);
    }
}