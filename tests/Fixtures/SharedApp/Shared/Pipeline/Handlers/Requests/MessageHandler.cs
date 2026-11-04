using Pipeline.Contracts.Requests;

namespace Pipeline.Handlers.Requests;

// The base of the message handlers of each module.
public abstract class MessageHandler<TMessage> where TMessage : Message {
    public abstract MessageId Handle(TMessage message);
}
