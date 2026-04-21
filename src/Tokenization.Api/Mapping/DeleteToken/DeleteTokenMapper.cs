using Tokenization.Api.Requests.v1;
using Tokenization.Application.Handlers.DeleteToken;

namespace Tokenization.Api.Mapping.DeleteToken;

/// <summary>
/// Provides mapping functionality between DeleteTokenRequest/DeleteTokenResponse and DeleteTokenCommand/TokenDto.
/// This mapper handles the conversion of token deletion requests from the API layer to application layer commands
/// and the conversion of application layer results back to API responses.
/// </summary>
internal class DeleteTokenMapper : IRequestMapper<DeleteTokenRequest, DeleteTokenCommand, bool, bool>
{
    /// <summary>
    /// Maps a DeleteTokenRequest from the API layer to a DeleteTokenCommand for the application layer.
    /// </summary>
    /// <param name="request">The API request containing token deletion details.</param>
    /// <returns>A DeleteTokenCommand containing the mapped data for processing by the application layer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request parameter is null.</exception>
    public DeleteTokenCommand MapRequest(DeleteTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new DeleteTokenCommand
        {
            Token = request.Token
        };
    }

    public bool MapResponse(bool result)
    {
        return result;
    }
}
