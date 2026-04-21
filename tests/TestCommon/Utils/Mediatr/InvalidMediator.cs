using MediatR;

namespace Tokenization.Tests.Shared.Utils.Mediatr;

// Helper class for testing exception handling
internal class InvalidMediator : IMediator
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Test exception for exception handling middleware");
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = new CancellationToken()) where TRequest : IRequest
    {
        throw new InvalidOperationException("Test exception for exception handling middleware");
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Test exception for exception handling middleware");
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Test exception for exception handling middleware");
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Test exception for exception handling middleware");
    }

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        return Task.CompletedTask;
    }
}
