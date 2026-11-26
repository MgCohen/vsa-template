namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public interface IRunnableHandler<in TMessage> : IHandler<TMessage> where TMessage : class {
    Task<IMessageResult> RunAsync(IEnvelope<TMessage> envelope);
}
