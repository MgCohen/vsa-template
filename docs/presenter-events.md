# Presenter events

A presenter is the interface between the views and one module. A module has one or more presenters, and a presenter never covers two modules. A view that shows more than one module uses the presenter of each module.

Changes go in two directions:

| Direction | Path |
|---|---|
| Top down | A view calls a presenter method. The presenter sends a request with `IRequestSender` (`Run`), and changes its state from the result. `Run` puts the message of the `Error` of the result on the `OpState` |
| Bottom up | A handler publishes an event. The presenter gets it (`IPresenterHandler<TEvent>`), changes its state, and raises `Changed` |

The bottom-up path works at the presenter level. It does not need a view: a view only listens to `Changed` and renders.

## Flow

```text
module handler ──PublishAsync──▶ InProcessEventPublisher
                                  1. the event pipeline: each IntegrationEventHandler<TEvent>, in the scope of the publish
                                  2. if the run is Accepted: each IIntegrationEventObserver ──▶ LivePresenters
                                       for each live presenter scope: add the delivery to the queue of the scope
                                         if the scope is idle: this flow runs the queue until it is empty
                                         if the scope is busy: the flow that runs the queue runs this delivery later
                                       one delivery:
                                         resolve IEnumerable<IPresenterHandler<TEvent>>
                                         for each handler, while the scope is live: HandleAsync(envelope), then Changed
PresenterWatcher ──Changed──▶ InvokeAsync(StateHasChanged)
```

## Parts

| Part | Project | Job |
|---|---|---|
| `IIntegrationEventObserver` | `Umpire.Vsa.Contracts.Events` | Gets each event after its handlers. The publisher calls it, so `Umpire.Vsa.Handlers.Events` needs no reference to `Umpire.Vsa.Presenters`. |
| `IIntegrationEventHandler<TEvent>` | `Umpire.Vsa.Handlers.Events` | The one handler shape for an event: `HandleAsync(Envelope<TEvent>)`. It has no `[AutoRegister]`. |
| `IPresenterHandler<TEvent>` | `Umpire.Vsa.Presenters` | A presenter implements it one time for each event that it shows. |
| `PresenterScope` | `Umpire.Vsa.Presenters` | Scoped service. Its first resolve in a scope makes that scope live. The end of the scope removes it. It has the queue of the deliveries to the scope. |
| `LivePresenters` | `Umpire.Vsa.Presenters` | The live presenter scopes, for the whole app. It implements the observer. `AddPresenters` registers it as a singleton. |
| `AddPresenters` | `Umpire.Vsa.Presenters` | Registers each presenter one time, as scoped. Forwards each `IPresenterHandler<TEvent>` of the presenter to that registration. |
| `PresenterWatcher` | `Umpire.Vsa.Views` | The base of a component that shows one or more presenters. `Watch(a, b)` resolves `PresenterScope`, listens to `Changed` of each presenter and renders. |
| `PresenterComponent<T>` | `Umpire.Vsa.Views` | A `PresenterWatcher` for one presenter, given as the `Presenter` parameter. |

## Presenter scopes

A scope gets events only when it is live. These parts resolve `PresenterScope`:

| App | Presenter scope | Resolves `PresenterScope` |
|---|---|---|
| Blazor | One for each circuit | The first `PresenterWatcher` of the circuit |
| Unity | One for each page, or one for the app | The shell or the page that makes the scope, one time |
| Test | The test scope | The test, with `GetRequiredService<PresenterScope>()` |

- Dispose each scope that resolves `PresenterScope`. A scope that you do not dispose stays live: it keeps its presenters and gets each event until the app stops. Blazor disposes the scope of a circuit at the end of the circuit. In Unity, the shell or the page that makes the scope disposes it.
- Resolve `PresenterScope` in a scope only. A resolve from the root provider throws `InvalidOperationException`, because a root `PresenterScope` stays live until the app stops.
- A scope gets the events that are published after it becomes live. A `PresenterWatcher` makes the scope live in `Watch`. If the first view of a circuit calls `Watch` late, for example after an `await` in `OnInitializedAsync`, the presenters of the circuit do not get the events of that time. Call `Watch` in `OnParametersSet`, before the first `await`. `PresenterComponent<T>` calls `Watch` in `OnParametersSet`.

## Rules

- Each presenter of a live scope gets its events, also when no view shows it. A scope gets no more events after its end.
- The delivery resolves the handlers in the live scope. If a presenter does not exist yet in that scope, the resolve makes it.
- A presenter constructor does not send requests. A resolve during a publish then cannot start a second publish.
- `LivePresenters` is one for the app. Each presenter of the event type gets the event, in each live scope (for example each circuit). In an app with more than one user, the presenter must ignore the events that are not for its user.
- Each scope has a queue of deliveries. The presenters of one scope get one event at a time, in the order of the queue. The handlers of one event have no order.
- If the scope is idle, the delivery runs on the thread of the publish, and the publish waits for it. If the scope is busy, the publish adds the delivery to the queue and does not wait. The flow that runs the queue then runs the delivery. A publish never waits for another flow, so two scopes that publish to each other cannot deadlock.
- The order of a response and its event depends on the sender of the request:

  | Sender of the request | State of the scope | Order |
  |---|---|---|
  | A presenter handler, during a delivery | Busy | The event goes in the queue. The response comes first |
  | A view or a presenter method, outside a delivery | Idle | The event runs during the publish. The event can come first |

  Make `HandleAsync` safe for a change that the presenter has already applied, for example replace by ID.
- The thread of a delivery is the thread of the flow that runs the queue. That flow can be another publish, not the publish of the event. In Unity, the presenters run on the main thread only if each publish runs on the main thread. A publish from `Task.Run` to an idle scope runs the presenters on a pool thread. A main-thread publish to a scope that a pool thread delivers to also runs on that pool thread.
- In Blazor, the flow of circuit X can run the presenters of circuit Y on the thread of X. The render of Y stays safe, because `PresenterWatcher` calls `InvokeAsync`. But the state of a presenter of Y can change while Y renders.
- The cost of a publish: the publish runs the presenters of each idle live scope, one scope after the other. It does not wait for a busy scope. The flow that runs the queue of a scope also runs the deliveries that other flows add during that time. Keep presenter handlers short: a slow handler makes each publish to an idle scope slow.
- The delivery checks that the scope is live before each handler. If the scope ends during the delivery, the next handlers do not run. An `ObjectDisposedException` of an ended scope is not an error.
- If a handler throws or returns an error, the log gets the error, and the next handler runs. If a `Changed` listener throws, the log gets the error, and the delivery continues.
- A delivery that runs later keeps the token of its publish. If that token is cancelled, an `OperationCanceledException` of the handler is not an error.
- `IIntegrationEventHandler<TEvent>` has no `[AutoRegister]`. The marker is on `IntegrationEventHandler<TEvent>` only, so the module scan does not take a presenter as a module handler.
- Each scope gets its own envelope. `Envelope.Services` is the provider of the presenter scope, and `Envelope.Cancellation` is the token of the publish.
- A view renders on its own thread: `PresenterWatcher` calls `InvokeAsync(StateHasChanged)`.
- The observer runs after the handlers. Publish an event after the change is saved, so that a view does not show a change that a rollback removes.
- A presenter does not publish events, and does not use `IIntegrationEventObserver` or `LivePresenters` (`UMP010204`). To change state, it sends a request with `IRequestSender`, and the request handler publishes the event.
- A presenter shows the events of its own module only. The definition lets `Presenters` use the `Contracts.Events` of its own module only (`UMP020101`).

## Example

```csharp
public sealed class BellPresenter(IRequestSender requests) : Presenter(requests), IPresenterHandler<Rang> {
    public string? Last { get; private set; }

    public Task<Result<Unit>> HandleAsync(Envelope<Rang> envelope) {
        Last = envelope.Message.Bell;
        return Task.FromResult<Result<Unit>>(Unit.Value);
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
