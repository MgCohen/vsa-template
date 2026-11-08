using VSA.Contracts.Requests;
using VSA.Domain;

namespace VSA.Handlers.Requests;

interface IRequestHandler<TResponse> where TResponse : IResponse {
    Task<Result<TResponse>> HandleAsync(IRequest<TResponse> request, CancellationToken ct);
}
