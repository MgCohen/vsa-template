using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Requests;

namespace Umpire.Vsa.Handlers.Requests;

[AutoRegister]
[RequireDerived]
[Preserve]
public abstract class RequestHandler<TRequest, TResponse> : IHandler<TRequest>
    where TRequest : class, IRequest<TResponse>
    where TResponse : IResponse {
    public abstract Task<Result<TResponse>> HandleAsync(Envelope<TRequest> envelope);

    async Task<IMessageResult> IHandler.HandleAsync(Envelope envelope) =>
        await HandleAsync((Envelope<TRequest>)envelope);
}
