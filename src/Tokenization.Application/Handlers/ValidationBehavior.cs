using FluentValidation;
using MediatR;

namespace Tokenization.Application.Handlers;

/// <summary>
/// MediatR pipeline behavior that runs all FluentValidation validators before handling a request.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>Executes all validators and throws exception when failures exist.</summary>
    /// <typeparam name="TResponse">Response type.</typeparam>
    /// <param name="request"><see cref="TRequest"/> to validate.</param>
    /// <param name="next">Delegate for next action in the pipeline.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="TResponse"/> object.</returns>
    /// <exception cref="ValidationException">Thrown when any validator contains a failure.</exception>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next(ct);
        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next(ct);
    }
}