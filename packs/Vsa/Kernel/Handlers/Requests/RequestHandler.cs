namespace Umpire.Vsa.Handlers.Requests;

using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Requests;

[AutoRegister]
[RequireDerived]
[Preserve]
public abstract class RequestHandler<TRequest, TResponse> : IHandler<TRequest>
    where TRequest : class, IRequest<TResponse>
    where TResponse : IResponse {
    public abstract Task<Result<TResponse>> HandleAsync(Envelope<TRequest> envelope);

    async Task<IMessageResult> IHandler<TRequest>.RunAsync(Envelope<TRequest> envelope) =>
        await HandleAsync(envelope);
}
