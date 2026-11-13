using VSA.Analyzers.Markers;
using VSA.Contracts;
using VSA.Contracts.Requests;

namespace VSA.Handlers.Requests;

[AutoRegister]
[RequireDerived]
public abstract class RequestHandler<TRequest, TResponse> : IHandler<TRequest>
    where TRequest : class, IRequest<TResponse>
    where TResponse : IResponse {
    public abstract Task<Result<TResponse>> HandleAsync(Envelope<TRequest> envelope);

    async Task<IMessageResult> IHandler.HandleAsync(Envelope envelope) =>
        await HandleAsync((Envelope<TRequest>)envelope);
}
