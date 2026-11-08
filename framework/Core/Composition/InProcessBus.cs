using VSA.Contracts.Requests;
using VSA.Handlers.Requests;
using VSA.Presenters;

namespace VSA.Composition;

sealed class InProcessBus(IServiceProvider provider) : IBus {
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        where TResponse : IResponse {
        if (request is null) {
            throw new ArgumentNullException(nameof(request));
        }
        var service = typeof(RequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = provider.GetService(service) as IRequestHandler<TResponse>
            ?? throw new InvalidOperationException(
                $"Request {request.GetType().FullName} has no handler. Add a RequestHandler<{request.GetType().Name}, "
                + $"{typeof(TResponse).Name}> to the Handlers.Requests project of its module.");
        var result = await handler.HandleAsync(request, ct);
        return result.Error is { } error ? throw new RequestException(error.Code, error.Message) : result.Value;
    }
}
