namespace Tokenization.Api.Mapping;

/// <summary>
/// Defines a contract for mapping between API requests/responses and application layer commands/results.
/// </summary>
/// <typeparam name="TRequest">The type of the incoming API request.</typeparam>
/// <typeparam name="TCommandRequest">The type of the command to be sent to the application layer.</typeparam>
/// <typeparam name="TCommandResult">The type of the result returned from the application layer.</typeparam>
/// <typeparam name="TResponse">The type of the API response to be returned.</typeparam>
internal interface IRequestMapper<in TRequest, out TCommandRequest, in TCommandResult, out TResponse>
{
    /// <summary>
    /// Maps an API request to an application layer command.
    /// </summary>
    /// <param name="request">The API request to map.</param>
    /// <returns>The mapped application layer command.</returns>
    TCommandRequest MapRequest(TRequest request);

    /// <summary>
    /// Maps an application layer result to an API response.
    /// </summary>
    /// <param name="cmd">The application layer result to map.</param>
    /// <returns>The mapped API response.</returns>
    TResponse MapResponse(TCommandResult cmd);
}
