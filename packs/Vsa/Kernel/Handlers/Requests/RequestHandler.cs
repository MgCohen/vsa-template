namespace Umpire.Vsa.Handlers.Requests;

using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Requests;

[AutoRegister]
public abstract class RequestHandler<TRequest, TResponse> : IRunnableHandler<TRequest>
    where TRequest : class, IRequest<TResponse>
    where TResponse : IResponse {
    public abstract Task<Result<TResponse>> HandleAsync(IEnvelope<TRequest> envelope);

    async Task<IMessageResult> IRunnableHandler<TRequest>.RunAsync(IEnvelope<TRequest> envelope) =>
        await HandleAsync(envelope);
}
