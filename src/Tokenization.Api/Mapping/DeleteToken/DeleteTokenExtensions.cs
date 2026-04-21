using Tokenization.Api.Requests.v1;
using Tokenization.Application.Handlers.DeleteToken;

namespace Tokenization.Api.Mapping.DeleteToken;

/// <summary>
/// Provides extension methods for converting between API request/response objects and application layer commands/DTOs.
/// </summary>
internal static class DeleteTokenExtensions
{
    /// <summary>
    /// Converts a DeleteTokenRequest to a DeleteTokenCommand using the DeleteTokenMapper.
    /// </summary>
    /// <param name="request">The DeleteTokenRequest to convert.</param>
    /// <returns>A DeleteTokenCommand containing the mapped data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request parameter is null.</exception>
    public static DeleteTokenCommand ToDeleteTokenCommand(this DeleteTokenRequest request)
    {
        return new DeleteTokenMapper().MapRequest(request);
    }
}
