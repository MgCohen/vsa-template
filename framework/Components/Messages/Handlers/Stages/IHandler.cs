namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public interface IHandler;

public interface IHandler<in TMessage> : IHandler where TMessage : class {
    Task<IMessageResult> RunAsync(IEnvelope<TMessage> envelope);
}
