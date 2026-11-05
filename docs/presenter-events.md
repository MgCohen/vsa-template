# Presenter events

A presenter is the interface between the views and one module. A module has one or more presenters, and a presenter never covers two modules. A view that shows more than one module uses the presenter of each module.

Changes go in two directions:

| Direction | Path |
|---|---|
| Top down | A view calls a presenter method. The presenter sends a request with `IBus` (`Run`), and changes its state from the response |
| Bottom up | A handler publishes an event. The presenter gets it (`IPresenterEvent<TEvent>`), changes its state, and raises `Changed` |

The bottom-up path works at the presenter level. It does not need a view: a view only listens to `Changed` and renders.

## Flow

```text
module handler ──PublishAsync──▶ InProcessEventPublisher
                                  1. each IntegrationEventHandler<TEvent>
                                  2. each IIntegrationEventObserver ──▶ LivePresenters
                                                                         for each presenter that implements IPresenterEvent<TEvent>:
                                                                           on the dispatcher of its scope (if any), one event at a time:
                                                                             OnEventAsync(event), then Changed
PresenterWatcher ──Changed──▶ InvokeAsync(StateHasChanged)
```

## Parts

| Part | Project | Job |
|---|---|---|
| `IIntegrationEventObserver` | `VSA.Contracts.Events` | Gets each event after its handlers. The publisher calls it, so `VSA.Handlers.Events` needs no reference to `VSA.Presenters` (it also targets `netstandard2.1`). |
| `IPresenterEvent<TEvent>` | `VSA.Presenters` | A presenter implements it one time for each event that it shows. |
| `LivePresenters` | `VSA.Presenters` | The presenters that exist now, for the whole app. It implements the observer. `AddVsaLivePresenters` registers it as a singleton. |
| `AddVsaPresenter(type)` | `VSA.Presenters` | Registers a scoped presenter. When the services make a presenter with `IPresenterEvent`, they add it to `LivePresenters`. The end of the scope disposes the presenter, which removes it. `AddVsa` calls it for each presenter. |
| `LivePresenters.Dispatch` | `VSA.Presenters` | Optional scoped service. The app sets the thread of the events, for example the main thread in Unity. |
| `PresenterWatcher` | `VSA.Views` | The base of a component that shows one or more presenters. `Watch(a, b)` listens to `Changed` of each presenter and renders. |
| `PresenterComponent<T>` | `VSA.Views` | A `PresenterWatcher` for one presenter, given as the `Presenter` parameter. |

## Rules

- Each presenter that exists gets its events, also when no view shows it. It gets no more events after its scope ends.
- `LivePresenters` is one for the app. Each presenter of the event type gets the event, in each scope (for example each circuit). In an app with more than one user, the presenter must ignore the events that are not for its user.
- The events of one presenter run one at a time. With a `LivePresenters.Dispatch` service in the scope, they run on that dispatcher.
- A view renders on its own thread: `PresenterWatcher` calls `InvokeAsync(StateHasChanged)`.
- The publisher does not wait for the presenters, and the token of the publisher does not go to the presenters. An error in one presenter goes to the log.
- The observer runs after the handlers, in the scope of the publisher. Publish an event after the change is saved, so that a view does not show a change that a rollback removes.
- A presenter that sends a request can get the event of that request before the response. Make `OnEventAsync` safe for a change that the presenter has already applied, for example replace by ID.
- A presenter does not publish events, and does not use `IIntegrationEventObserver` or `LivePresenters` (`VSA1004`). To change state, it sends a request with `IBus`, and the request handler publishes the event.
- A presenter shows the events of its own module only. The layer map gives `Presenters` the `Contracts.Events` of its own module.

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

A component for one presenter:

```razor
@inherits PresenterComponent<BellPresenter>
<p>@Presenter.Last</p>
```

A component that combines two modules:

```csharp
public sealed partial class TrayView : PresenterWatcher {
    [Parameter, EditorRequired] public HandPresenter Hand { get; set; } = default!;
    [Parameter, EditorRequired] public PoolPresenter Pool { get; set; } = default!;

    protected override void OnParametersSet() => Watch(Hand, Pool);
}
```
