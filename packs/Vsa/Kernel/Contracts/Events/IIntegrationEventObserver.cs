namespace Umpire.Vsa.Contracts.Events;

// Gets each event after its handlers ran. The live presenters (Umpire.Vsa.Presenters) implement it, so a view shows the events of the modules.
// Here, so the publisher calls it with no reference to Umpire.Vsa.Presenters: Umpire.Vsa.Handlers.Events also targets netstandard2.1, for Unity.
public interface IIntegrationEventObserver {
    Task OnPublishedAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent;
}
