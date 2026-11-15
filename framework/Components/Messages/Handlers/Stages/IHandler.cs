using VSA.Contracts;

namespace VSA.Handlers;

public interface IHandler {
    Task<IMessageResult> HandleAsync(Envelope envelope);
}

public interface IHandler<TMessage> : IHandler where TMessage : class;
