using VSA.Contracts.Requests;
using VSA.Domain;

namespace VSA.Handlers.Requests;

public abstract class RequestHandler<TRequest, TResponse> : IRequestHandler<TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResponse {
    Task<Result<TResponse>> IRequestHandler<TResponse>.HandleAsync(IRequest<TResponse> request, CancellationToken ct) =>
        HandleRequestAsync((TRequest)request, ct);

    protected abstract Task<Result<TResponse>> HandleRequestAsync(TRequest request, CancellationToken ct);
}
