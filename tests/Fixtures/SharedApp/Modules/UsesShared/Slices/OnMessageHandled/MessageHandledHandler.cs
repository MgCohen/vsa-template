using Pipeline.Slices.Messages;
using VSA.Handlers.Events;

namespace UsesShared.Slices.OnMessageHandled;

// The module has no events of its own, so it gets the Shared event from VSA.Sdk directly.
sealed class MessageHandledHandler : IntegrationEventHandler<MessageHandled> {
    public override Task HandleAsync(MessageHandled integrationEvent, CancellationToken ct) => Task.CompletedTask;
}
