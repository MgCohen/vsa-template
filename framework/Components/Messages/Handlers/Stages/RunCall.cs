namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

sealed class RunCall<TMessage> : HandlerCall<IRunnableHandler<TMessage>, TMessage> where TMessage : class {
    public override Task<IMessageResult> CallAsync(IRunnableHandler<TMessage> handler, IEnvelope<TMessage> envelope) =>
        handler.RunAsync(envelope);
}
