namespace Spike.Pack;

using Spike.Core;

public interface IRequestHandler {
    Task<object?> Handle(object request, CancellationToken ct);
}

public abstract class RequestHandler<TRequest, TResponse> : IRequestHandler where TRequest : IRequest<TResponse> {
    public abstract Task<TResponse> Handle(TRequest request, CancellationToken ct);

    async Task<object?> IRequestHandler.Handle(object request, CancellationToken ct) =>
        await Handle((TRequest)request, ct).ConfigureAwait(false);
}
