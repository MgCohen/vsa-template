using Pipeline.Contracts.Events;
using Pipeline.Handlers.Events.Audit;

namespace UsesPlugins.Slices.AuditMessages;

// A module handler that inherits the plugin base.
sealed class MessageAuditHandler : MessageAudit {
    public override AuditConfig Config { get; } = new(FromId: 1);

    protected override Task Audit(MessageHandled message, CancellationToken ct) => Task.CompletedTask;
}
