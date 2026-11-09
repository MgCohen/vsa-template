using Pipeline.Contracts.Requests;
using VSA.Contracts;
using VSA.Contracts.Requests;
using VSA.Handlers.Requests;

namespace Pipeline.Handlers.Requests;

// The base of the message handlers of each module. The router finds RequestHandler through this base.
public abstract class MessageHandler<TMessage> : RequestHandler<TMessage, MessageId> where TMessage : Message, IRequest<MessageId> {
    public abstract MessageId Handle(TMessage message);

    public sealed override Task<Result<MessageId>> HandleAsync(Envelope<TMessage> envelope) =>
        Task.FromResult<Result<MessageId>>(Handle(envelope.Message));
}
