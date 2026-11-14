using VSA.Contracts.Events;

namespace VSA.Presenters;

// A presenter that shows an event of a module. Implement it once for each event type.
// LivePresenters calls OnEventAsync, one event at a time for each presenter, then the presenter raises Changed.
// Each presenter that exists gets the event, also when no view shows it. A presenter does not publish events (VSA1004).
// If the presenter also sends the request that publishes the event, the event can come before the response:
// make OnEventAsync safe to run for a change that the presenter has already applied, for example replace by ID.
public interface IPresenterEvent<TEvent> where TEvent : IntegrationEvent {
    Task OnEventAsync(TEvent integrationEvent, CancellationToken ct);
}
