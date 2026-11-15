namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public interface IHandler {
    Task<IMessageResult> HandleAsync(Envelope envelope);
}

public interface IHandler<TMessage> : IHandler where TMessage : class;
