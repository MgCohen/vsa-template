using Pipeline.Contracts.Requests;
using VSA.Contracts.Requests;
using VSA.Domain;
using VSA.Handlers.Requests;

namespace Pipeline.Handlers.Requests;

// The base of the message handlers of each module. The router finds RoutedHandler through this base.
public abstract class MessageHandler<TMessage> : RoutedHandler<TMessage, MessageId> where TMessage : Message, IRequest<MessageId> {
    public abstract MessageId Handle(TMessage message);

    protected sealed override Task<Result<MessageId>> HandleRequestAsync(TMessage request, CancellationToken ct) =>
        Task.FromResult<Result<MessageId>>(Handle(request));
}
