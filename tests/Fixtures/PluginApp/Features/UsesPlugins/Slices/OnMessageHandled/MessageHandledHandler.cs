using Pipeline.Contracts.Events;
using VSA.Contracts;
using VSA.Handlers.Events;

namespace UsesPlugins.Slices.OnMessageHandled;

// The module has no events of its own, so it gets the plugin event from VSA.Sdk directly.
sealed class MessageHandledHandler : IntegrationEventHandler<MessageHandled> {
    public override Task<Result<Unit>> HandleAsync(Envelope<MessageHandled> envelope) => Done;
}
