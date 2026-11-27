namespace Spike.Pack;

using Spike.Core;

public abstract class RequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse> {
    public abstract Task<TResponse> Handle(TRequest request, CancellationToken ct);
}
