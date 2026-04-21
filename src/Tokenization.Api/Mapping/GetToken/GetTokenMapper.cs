using Tokenization.Api.Requests.v1;
using Tokenization.Api.Responses;
using Tokenization.Application.Handlers.GetToken;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Api.Mapping.GetToken;

/// <summary>
/// Provides mapping functionality between GetTokenRequest/GetTokenResponse and GetTokenCommand/TokenDto.
/// This mapper handles the conversion of token deactivation requests from the API layer to application layer commands
/// and the conversion of application layer results back to API responses.
/// </summary>
internal class GetTokenMapper
    : IRequestMapper<GetTokenRequest, GetTokenCommand, TokenSummary, GetTokenResponse>
{
    /// <summary>
    /// Maps a GetTokenRequest from the API layer to a GetTokenCommand for the application layer.
    /// </summary>
    /// <param name="request">The API request containing token deactivation details.</param>
    /// <returns>A GetTokenCommand containing the mapped data for processing by the application layer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request parameter is null.</exception>
    public GetTokenCommand MapRequest(GetTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        return new GetTokenCommand
        { 
            Token = request.Token
        };
    }

    /// <summary>
    /// Maps a TokenSummary from the application layer to a GetTokenResponse for the API layer.
    /// </summary>
    /// <param name="summary">The non-sensitive token summary.</param>
    /// <returns>A GetTokenResponse containing the mapped data for the API response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the dto parameter is null.</exception>
    public GetTokenResponse MapResponse(TokenSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        
        return new GetTokenResponse
        {
            Token = summary.Token,
            MaskedData = summary.MaskedData,
            Last4 = summary.Last4,
            PaymentMethodType = summary.PaymentMethodType.ToString(),
            Network = summary.Network,
            CustomerId = summary.CustomerId,
            TenantId = summary.TenantId,
            CreatedAt = summary.CreatedAt,
            ExpiresAt = summary.ExpiresAt,
            MaxUses = summary.MaxUses,
            UsageCount = summary.UsageCount
        };
    }
}