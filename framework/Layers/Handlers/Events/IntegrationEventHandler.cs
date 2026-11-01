using VSA.Contracts.Events;

namespace VSA.Handlers.Events;

public abstract class IntegrationEventHandler<TEvent> where TEvent : IntegrationEvent {
    public abstract Task HandleAsync(TEvent integrationEvent, CancellationToken ct);
}
