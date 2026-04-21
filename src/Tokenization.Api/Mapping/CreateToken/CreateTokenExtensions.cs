using Tokenization.Api.Requests.v1;
using Tokenization.Api.Responses;
using Tokenization.Application.Handlers.CreateToken;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Api.Mapping.CreateToken;

/// <summary>
/// Provides extension methods for converting between API request/response objects and application layer commands/DTOs.
/// </summary>
internal static class CreateTokenExtensions
{
    /// <summary>
    /// Converts a CreateTokenRequest to a CreateTokenCommand using the CreateTokenMapper.
    /// </summary>
    /// <param name="request">The CreateTokenRequest to convert.</param>
    /// <param name="tenantContextService">The tenant context service used to resolve the current tenant ID.</param>
    /// <returns>A CreateTokenCommand containing the mapped data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request parameter is null.</exception>
    public static CreateTokenCommand ToCreateTokenCommand(
        this CreateTokenRequest request,
        ITenantContextService tenantContextService)
    {
        return new CreateTokenMapper(tenantContextService).MapRequest(request);
    }

    /// <summary>
    /// Converts a TokenDto to a CreateTokenResponse using the CreateTokenMapper.
    /// </summary>
    /// <param name="dto">The TokenDto to convert.</param>
    /// <param name="tenantContextService">The tenant context service used to resolve the current tenant ID.</param>
    /// <returns>A CreateTokenResponse containing the mapped data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the dto parameter is null.</exception>
    public static CreateTokenResponse ToCreateTokenResponse(
        this TokenSummary dto,
        ITenantContextService tenantContextService)
    {
        return new CreateTokenMapper(tenantContextService).MapResponse(dto);
    }
}
