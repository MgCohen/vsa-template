using VSA.Contracts.Requests;

namespace VSA.Presenters;

// An interface: in process (default), HTTP (views in a separate app), and a fake in tests.
// If the request fails, Send throws RequestException.
public interface IBus {
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) where TResponse : IResponse;
}
