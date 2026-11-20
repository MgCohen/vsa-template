namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public interface IHandler;

public interface IHandler<TMessage> : IHandler where TMessage : class {
    Task<IMessageResult> RunAsync(Envelope<TMessage> envelope);
}
