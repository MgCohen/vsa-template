namespace Umpire.Vsa.Contracts.Events;

public interface IIntegrationEventObserver {
    Task OnPublishedAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent;
}
