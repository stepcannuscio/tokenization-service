using Tokenization.Api.Requests.v1;
using Tokenization.Api.Responses;
using Tokenization.Application.Handlers.DetokenizeToken;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Api.Mapping.DetokenizeToken;

/// <summary>
/// Provides mapping functionality between DetokenizeTokenRequest/DetokenizeTokenResponse and DetokenizeTokenCommand/TokenDto.
/// This mapper handles the conversion of token deactivation requests from the API layer to application layer commands
/// and the conversion of application layer results back to API responses.
/// </summary>
internal class DetokenizeTokenMapper
    : IRequestMapper<DetokenizeTokenRequest, DetokenizeTokenCommand, DetokenizedToken, DetokenizeTokenResponse>
{
    /// <summary>
    /// Maps a DetokenizeTokenRequest from the API layer to a DetokenizeTokenCommand for the application layer.
    /// </summary>
    /// <param name="request">The API request containing token deactivation details.</param>
    /// <returns>A DetokenizeTokenCommand containing the mapped data for processing by the application layer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request parameter is null.</exception>
    public DetokenizeTokenCommand MapRequest(DetokenizeTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new DetokenizeTokenCommand
        {
            Token = request.Token
        };
    }

    /// <summary>
    /// Maps a TokenSummary from the application layer to a DetokenizeTokenResponse for the API layer.
    /// </summary>
    /// <param name="detokenizedToken">The detokenized token details.</param>
    /// <returns>A DetokenizeTokenResponse containing the mapped data for the API response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the dto parameter is null.</exception>
    public DetokenizeTokenResponse MapResponse(DetokenizedToken detokenizedToken)
    {
        ArgumentNullException.ThrowIfNull(detokenizedToken);

        var cardPlainText = detokenizedToken.ToCardPlaintext();

        return new DetokenizeTokenResponse
        {
            Pan = cardPlainText.Pan,
            ExpMonth = cardPlainText.ExpMonth,
            ExpYear = cardPlainText.ExpYear,
            CardholderName = cardPlainText.CardholderName,
            PaymentMethodType = detokenizedToken.TokenSummary.PaymentMethodType.ToString(),
            Network = detokenizedToken.TokenSummary.Network
        };
    }
}
