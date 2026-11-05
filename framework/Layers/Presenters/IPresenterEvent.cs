using VSA.Contracts.Events;

namespace VSA.Presenters;

// A presenter that shows an event of a module. Implement it once for each event type.
// LivePresenters calls OnEventAsync on the dispatcher of the view, then the presenter raises Changed.
// Only presenters that a view shows now get the event. A presenter does not publish events (VSA1004).
// If the presenter also sends the request that publishes the event, the event can come before the response:
// make OnEventAsync safe to run for a change that the presenter has already applied, for example replace by ID.
public interface IPresenterEvent<TEvent> where TEvent : IntegrationEvent {
    Task OnEventAsync(TEvent integrationEvent, CancellationToken ct);
}
