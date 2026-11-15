namespace Umpire.Vsa.Composition;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Presenters;

sealed class InProcessBus(IServiceProvider provider) : IBus {
    public Task<Result<TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        where TResponse : IResponse =>
        RequestDispatch.SendAsync(provider, request ?? throw new ArgumentNullException(nameof(request)), ct);
}
