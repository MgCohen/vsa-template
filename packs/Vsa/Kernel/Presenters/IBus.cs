using VSA.Contracts;
using VSA.Contracts.Requests;

namespace VSA.Presenters;

// An interface: in process (default), HTTP (views in a separate app), and a fake in tests.
// Send gives the result of the request: the response, or the Error of the handler.
public interface IBus {
    Task<Result<TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        where TResponse : IResponse;
}
