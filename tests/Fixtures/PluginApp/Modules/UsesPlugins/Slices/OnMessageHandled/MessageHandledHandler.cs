using Pipeline.Contracts.Events;
using VSA.Handlers.Events;

namespace UsesPlugins.Slices.OnMessageHandled;

// The module has no events of its own, so it gets the plugin event from VSA.Sdk directly.
sealed class MessageHandledHandler : IntegrationEventHandler<MessageHandled> {
    public override Task HandleAsync(MessageHandled integrationEvent, CancellationToken ct) => Task.CompletedTask;
}
