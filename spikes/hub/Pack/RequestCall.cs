namespace Spike.Pack;

using Spike.Core;

sealed class RequestCall<TRequest, TResponse> : IHandlerCall where TRequest : IRequest<TResponse> {
    public async Task<object?> Call(object handler, object message, CancellationToken ct) =>
        await ((RequestHandler<TRequest, TResponse>)handler).Handle((TRequest)message, ct).ConfigureAwait(false);
}
