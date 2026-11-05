# Presenter events

A view shows one presenter. The presenter changes its state after a request, and also after an event of a module. A presenter event is an integration event that a presenter shows.

## Flow

```text
module handler ──PublishAsync──▶ InProcessEventPublisher
                                  1. each IntegrationEventHandler<TEvent>
                                  2. each IIntegrationEventObserver ──▶ LivePresenters
                                                                         for each live presenter that implements IPresenterEvent<TEvent>:
                                                                           on the dispatcher of its view:
                                                                             OnEventAsync(event), then Changed
PresenterComponent ──Changed──▶ StateHasChanged
```

## Parts

| Part | Project | Job |
|---|---|---|
| `IIntegrationEventObserver` | `VSA.Contracts.Events` | Gets each event after its handlers. The publisher calls it, so `VSA.Handlers.Events` needs no reference to `VSA.Presenters` (it also targets `netstandard2.1`). |
| `IPresenterEvent<TEvent>` | `VSA.Presenters` | A presenter implements it once for each event that it shows. |
| `LivePresenters` | `VSA.Presenters` | The presenters that views show now. It implements the observer. `AddVsa` registers it as a singleton. |
| `PresenterComponent<T>` | `VSA.Views` | Adds its presenter to `LivePresenters`, with its own dispatcher, and removes it on dispose. |

## Rules

- Only a live presenter gets the event: a presenter that no view shows gets nothing.
- `LivePresenters` is one for the app. Each live presenter of the event type gets the event, in each circuit. In an app with more than one user, the presenter must ignore the events that are not for its user.
- A presenter that two views show gets each event one time.
- `OnEventAsync` runs on the dispatcher of the first view, so the presenter changes its state on the thread of the view.
- The publisher does not wait for the views, and the token of the publisher does not go to the presenters. An error in one presenter goes to the log.
- The observer runs after the handlers, in the scope of the publisher. Publish an event after the change is saved, so that a view does not show a change that a rollback removes.
- A presenter that sends a request can get the event of that request before the response. Make `OnEventAsync` safe for a change that the presenter has already applied, for example replace by ID.
- A presenter does not publish events, and does not use `IIntegrationEventObserver` or `LivePresenters` (`VSA1004`). To change state, it sends a request with `IBus`, and the request handler publishes the event.
- The layer map gives `Presenters` the `Contracts.Events` of each module, as it does for `Handlers.Events`.

## Example

```csharp
public sealed class BellPresenter(IBus bus) : Presenter(bus), IPresenterEvent<Rang> {
    public string? Last { get; private set; }

    public Task OnEventAsync(Rang integrationEvent, CancellationToken ct) {
        Last = integrationEvent.Bell;
        return Task.CompletedTask;
    }
}
```
