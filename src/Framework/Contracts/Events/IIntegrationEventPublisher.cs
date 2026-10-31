namespace VSA.Contracts.Events;

// Here, so request handlers can publish without a reference to Handlers.Events.
// An interface: in process now, a broker later, a fake in tests.
public interface IIntegrationEventPublisher {
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent;
}
