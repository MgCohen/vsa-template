namespace Umpire.Vsa.Handlers.Requests;

using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Requests;

public sealed class RequestHandlerCall<TRequest, TResponse>
    : HandlerCall<RequestHandler<TRequest, TResponse>, TRequest>
    where TRequest : class, IRequest<TResponse>
    where TResponse : IResponse {
    public override async Task<IMessageResult> CallAsync(
        RequestHandler<TRequest, TResponse> handler, IEnvelope<TRequest> envelope) =>
        await handler.HandleAsync(envelope);
}
