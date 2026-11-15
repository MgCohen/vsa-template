using Umpire.Messages.Contracts;

namespace Umpire.Messages.Handlers;

public interface IHandler {
    Task<IMessageResult> HandleAsync(Envelope envelope);
}

public interface IHandler<TMessage> : IHandler where TMessage : class;
