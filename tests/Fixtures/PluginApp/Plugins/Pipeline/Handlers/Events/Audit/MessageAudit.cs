using Pipeline.Contracts.Events;
using VSA.Contracts;
using VSA.Handlers.Events;

namespace Pipeline.Handlers.Events.Audit;

// A plugin base of event handlers. It handles a plugin event, so this Handlers.Events project needs
// the Contracts.Events project of its own plugin module.
public abstract class MessageAudit : IntegrationEventHandler<MessageHandled> {
    public abstract AuditConfig Config { get; }

    public sealed override async Task<Result<Unit>> HandleAsync(Envelope<MessageHandled> envelope) {
        if (envelope.Message.MessageId >= Config.FromId) {
            await Audit(envelope.Message, envelope.Cancellation);
        }
        return Unit.Value;
    }

    protected abstract Task Audit(MessageHandled message, CancellationToken ct);
}
