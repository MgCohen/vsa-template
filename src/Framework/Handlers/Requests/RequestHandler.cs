using VSA.Contracts.Requests;
using VSA.Domain;

namespace VSA.Handlers.Requests;

public abstract class RequestHandler<TRequest, TResponse> where TRequest : Request<TResponse> {
    public abstract Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken ct);
}
