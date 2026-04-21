using Asp.Versioning;
using MediatR;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Authorization.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tokenization.Api.Authorization;
using Tokenization.Api.Mapping.CreateToken;
using Tokenization.Api.Mapping.DeleteToken;
using Tokenization.Api.Mapping.DetokenizeToken;
using Tokenization.Api.Mapping.GetToken;
using Tokenization.Api.Requests.v1;
using Tokenization.Api.Responses;

namespace Tokenization.Api.Controllers;

/// <summary>
/// Controller for managing payment tokens with multi-tenant isolation.
/// All operations are automatically scoped to the authenticated user's tenant context.
/// </summary>
/// <remarks>
/// <list type="bullet">
///     <listheader><term>Multi-Tenant Security</term></listheader>
///     <item><description>The tenant ID is automatically extracted from the JWT token's <c>tenant_id</c> claim.</description></item>
///     <item><description>Legacy <c>merchant_id</c> claims are accepted and normalized.</description></item>
///     <item><description>Users can only create tokens belonging to their own tenant.</description></item>
///     <item><description>Cross-tenant access is strictly prohibited.</description></item>
/// </list>
/// <list type="bullet">
///     <listheader><term>API Versioning</term></listheader>
///     <item><description>Current version: v1.0</description></item>
///     <item><description>URL path versioning: /api/v1/tokens</description></item>
///     <item><description>Header versioning: X-API-Version: 1.0</description></item>
///     <item><description>Query parameter versioning: ?api-version=1.0</description></item>
/// </list>
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tokens")]
[EnableRateLimiting("AuthenticatedPolicy")]
[TenantCurrentContextAccess]
[Produces("application/json")]
public sealed class TokensController(
    IMediator mediator,
    ITenantContextService tenantContextService) : ControllerBase
{
    /// <summary>
    /// Retrieves token information by token ID.
    /// </summary>
    /// <param name="tokenId">The token identifier to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Token information including masked data and metadata.</returns>
    [HttpGet("{tokenId}")]
    [Authorize(Policy = Policies.CanReadTokens)]
    [ProducesResponseType(typeof(GetTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetToken(string tokenId, CancellationToken ct)
    {
        var request = new GetTokenRequest { Token = tokenId };
        var cmd = request.ToGetTokenCommand();
        var dto = await mediator.Send(cmd, ct);
        var response = dto.ToGetTokenResponse();

        return Ok(response);
    }

    /// <summary>
    /// Creates a token from plaintext payment data.
    /// </summary>
    /// <param name="request">The token creation request containing payment details and customer information.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A token creation response containing the generated token and masked payment information.</returns>
    [HttpPost]
    [Authorize(Policy = Policies.CanCreateTokens)]
    [EnableRateLimiting("TokenCreationPolicy")]
    [ProducesResponseType(typeof(CreateTokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateToken([FromBody] CreateTokenRequest request, CancellationToken ct)
    {
        var cmd = request.ToCreateTokenCommand(tenantContextService);
        var dto = await mediator.Send(cmd, ct);
        var response = dto.ToCreateTokenResponse(tenantContextService);
        var version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        return CreatedAtAction(
            nameof(GetToken),
            new { version, tokenId = response.Token },
            response);
    }
    
    /// <summary>
    /// Deletes a token.
    /// </summary>
    /// <param name="tokenId">The token to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{tokenId}")]
    [Authorize(Policy = Policies.CanDeleteTokens)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteToken(string tokenId, CancellationToken ct)
    {
        var request = new DeleteTokenRequest { Token = tokenId };
        var cmd = request.ToDeleteTokenCommand();
        var result = await mediator.Send(cmd, ct);

        return result ? NoContent() : NotFound();
    }
    
    /// <summary>
    /// Detokenizes a token to plaintext payment data.
    /// </summary>
    /// <param name="tokenId">The token to detokenize.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The plaintext payment data and token details.</returns>
    [HttpPost("{tokenId}/detokenize")]
    [Authorize(Policy = Policies.CanDetokenizeTokens)]
    [EnableRateLimiting("TokenDetokenizationPolicy")]
    [ProducesResponseType(typeof(DetokenizeTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DetokenizeToken(string tokenId, CancellationToken ct)
    {
        var request = new DetokenizeTokenRequest { Token = tokenId };
        var cmd = request.ToDetokenizeTokenCommand();
        var dto = await mediator.Send(cmd, ct);
        var response = dto.ToDetokenizeTokenResponse();

        return Ok(response);
    }
}
