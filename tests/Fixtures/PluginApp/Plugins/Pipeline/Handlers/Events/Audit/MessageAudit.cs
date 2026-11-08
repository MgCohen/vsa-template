using Pipeline.Contracts.Events;
using VSA.Handlers.Events;

namespace Pipeline.Handlers.Events.Audit;

// A plugin base of event handlers. It handles a plugin event, so this Handlers.Events project needs
// the Contracts.Events project of its own plugin module.
public abstract class MessageAudit : IntegrationEventHandler<MessageHandled> {
    public abstract AuditConfig Config { get; }

    public sealed override Task HandleAsync(MessageHandled integrationEvent, CancellationToken ct) =>
        integrationEvent.MessageId >= Config.FromId ? Audit(integrationEvent, ct) : Task.CompletedTask;

    protected abstract Task Audit(MessageHandled message, CancellationToken ct);
}
