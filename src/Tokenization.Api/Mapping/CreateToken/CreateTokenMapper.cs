using Tokenization.Api.Requests.v1;
using Tokenization.Api.Responses;
using Tokenization.Application.Handlers.CreateToken;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Api.Mapping.CreateToken;

/// <summary>
/// Provides mapping functionality between CreateTokenRequest/CreateTokenResponse and CreateTokenCommand/TokenDto.
/// This mapper handles the conversion of token creation requests from the API layer to application layer commands
/// and the conversion of application layer results back to API responses.
/// </summary>
internal class CreateTokenMapper(ITenantContextService tenantContext)
    : IRequestMapper<CreateTokenRequest, CreateTokenCommand, TokenSummary, CreateTokenResponse>
{
    private readonly ITenantContextService _tenantContext =
        tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));

    /// <summary>
    /// Maps a CreateTokenRequest from the API layer to a CreateTokenCommand for the application layer.
    /// </summary>
    /// <param name="request">The API request containing token creation details.</param>
    /// <returns>A CreateTokenCommand containing the mapped data for processing by the application layer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request parameter is null.</exception>
    public CreateTokenCommand MapRequest(CreateTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Get the current tenant ID from the authenticated user's context
        var tenantId = _tenantContext.GetCurrentTenantId();

        return new CreateTokenCommand
        {
            TenantId = tenantId,
            CustomerId = request.CustomerId,
            PaymentMethodType = request.PaymentMethodType,
            TokenType = request.TokenType,
            StoredCredentialInitiator = request.StoredCredentialInitiator,
            StoredCredentialReason = request.StoredCredentialReason,
            Network = request.Network,
            Currency = request.Currency,
            Country = request.Country,
            ExpiresAt = request.ExpiresAt,
            MaxUses = request.MaxUses,
            InitialTransactionId = request.InitialTransactionId,
            Card = new CardPlaintext
            {
                CardholderName = request.CardholderName,
                ExpMonth = request.ExpirationMonth,
                ExpYear = request.ExpirationYear,
                Pan = request.Pan
            }
        };
    }

    /// <summary>
    /// Maps a TokenSummary from the application layer to a CreateTokenResponse for the API layer.
    /// </summary>
    /// <param name="summary">The token summary.</param>
    /// <returns>A CreateTokenResponse containing the mapped data for the API response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the dto parameter is null.</exception>
    public CreateTokenResponse MapResponse(TokenSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new CreateTokenResponse
        {
            Token = summary.Token,
            MaskedData = summary.MaskedData,
            Last4 = summary.Last4,
            PaymentMethodType = summary.PaymentMethodType.ToString(),
            Network = summary.Network
        };
    }
}
