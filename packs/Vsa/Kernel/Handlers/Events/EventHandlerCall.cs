namespace Umpire.Vsa.Handlers.Events;

using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Events;

public sealed class EventHandlerCall<TEvent> : HandlerCall<IIntegrationEventHandler<TEvent>, TEvent>
    where TEvent : IntegrationEvent {
    public override async Task<IMessageResult> CallAsync(
        IIntegrationEventHandler<TEvent> handler, IEnvelope<TEvent> envelope) =>
        await handler.HandleAsync(envelope);
}
