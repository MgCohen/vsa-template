namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public abstract class HandlerCall<THandler, TMessage> : IHandlerCall
    where THandler : class
    where TMessage : class {
    Type IHandlerCall.Handler => typeof(THandler);

    public abstract Task<IMessageResult> CallAsync(THandler handler, IEnvelope<TMessage> envelope);

    Task<IMessageResult> IHandlerCall.CallAsync(object handler, Envelope envelope) =>
        CallAsync((THandler)handler, (IEnvelope<TMessage>)envelope);
}
