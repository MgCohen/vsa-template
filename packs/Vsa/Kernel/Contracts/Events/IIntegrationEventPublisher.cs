namespace Umpire.Vsa.Contracts.Events;

using Umpire.Messages.Contracts;

// Here, so request handlers can publish without a reference to Handlers.Events.
// An interface: in process now, a broker later, a fake in tests.
public interface IIntegrationEventPublisher {
    Task<Result<Unit>> PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent;
}
