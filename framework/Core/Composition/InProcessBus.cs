using VSA.Contracts;
using VSA.Contracts.Requests;
using VSA.Handlers.Requests;
using VSA.Presenters;

namespace VSA.Composition;

sealed class InProcessBus(IServiceProvider provider) : IBus {
    public Task<Result<TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        where TResponse : IResponse =>
        RequestDispatch.SendAsync(provider, request ?? throw new ArgumentNullException(nameof(request)), ct);
}
