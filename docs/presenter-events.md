# Presenter events

A presenter is the interface between the views and one module. A module has one or more presenters, and a presenter never covers two modules. A view that shows more than one module uses the presenter of each module.

Changes go in two directions:

| Direction | Path |
|---|---|
| Top down | A view calls a presenter method. The presenter sends a request with `IRequestSender` (`Run`), and changes its state from the result. `Run` puts the message of the `Error` of the result on the `OpState` |
| Bottom up | A handler publishes an event. The presenter gets it (`IPresenterHandler<TEvent>`), changes its state, and raises `Changed` |

The bottom-up path works at the presenter level. It does not need a view: a view only listens to `Changed` and renders.

## Flow

The delivery to the presenters is a pipeline on the bus, the same as each other kind (`docs/handler-pipeline.md`, "Broadcast").

```text
module handler ──PublishAsync──▶ Pipelines.PublishAsync
                                  1. the event pipeline: each IntegrationEventHandler<TEvent>, in the scope of the publish
                                  2. if 1 is Accepted: the presenter pipeline, HandlersOf(IPresenterHandler<>)
                                       LiveScopesRunner: for each live presenter scope
                                         add the delivery to the queue of the scope, with an envelope of that scope
                                         if the scope is idle: this flow runs the queue until it is empty
                                         if the scope is busy: the flow that runs the queue runs this delivery later
                                       one delivery: each presenter of the event, then Changed of that presenter
PresenterWatcher ──Changed──▶ InvokeAsync(StateHasChanged)
```

## Parts

| Part | Project | Job |
|---|---|---|
| `IIntegrationEventHandler<TEvent>` | `Umpire.Vsa.Handlers.Events` | The one handler shape for an event: `HandleAsync(IEnvelope<TEvent>)`. It has no `[AutoRegister]` |
| `IPresenterHandler<TEvent>` | `Umpire.Vsa.Presenters` | A presenter implements it one time for each event that it shows. Its `RunAsync` raises `Changed` after `HandleAsync`, also when it fails |
| `Presenter` | `Umpire.Vsa.Presenters` | The base of each presenter. It has `[AutoRegister]`, so the core step registers each presenter as scoped |
| `PresenterScope` | `Umpire.Vsa.Presenters` | Scoped service. Its first resolve in a scope makes that scope live. The end of the scope removes it. It has the queue of the deliveries to the scope |
| `LiveScopes` | `Umpire.Vsa.Presenters` | The live presenter scopes, for the whole app |
| `LiveScopesRunner` | `Umpire.Vsa.Presenters` | The runner of the presenter pipeline. It runs the pipeline one time in each live scope, and logs each error |
| `AddPresenters` | `Umpire.Vsa.Presenters` | Adds `LiveScopes`, `PresenterScope` and the presenter pipeline. A presenter with a lifetime other than `Scoped` stops the start |
| `PresenterWatcher` | `Umpire.Vsa.Views` | The base of a component that shows one or more presenters. `Watch(a, b)` resolves `PresenterScope`, listens to `Changed` of each presenter and renders |
| `PresenterComponent<T>` | `Umpire.Vsa.Views` | A `PresenterWatcher` for one presenter, given as the `Presenter` parameter |

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
- `LiveScopes` is one for the app. Each presenter of the event type gets the event, in each live scope (for example each circuit). In an app with more than one user, the presenter must ignore the events that are not for its user.
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
- The runner checks that the scope is live before the delivery. If the scope ends during the delivery, the next handlers cannot resolve, and the runner logs no error for an ended scope.
- If a handler throws or returns an error, the next handler runs. The log gets each exception with its stack trace (`Envelope.HandlerExceptions`). If no handler throws, it gets the first error of the delivery.
- `Changed` is raised after each handler that completes or throws, but not after a cancellation. A `Changed` listener that throws is an error of its handler. If the handler and the listener both throw, the log gets an `AggregateException` with both.
- A delivery that starts later keeps the token of its publish. If that token is cancelled, an `OperationCanceledException` stops the delivery to that scope, and it is not an error.
- `IIntegrationEventHandler<TEvent>` has no `[AutoRegister]`. The event pipeline takes the classes of `IntegrationEventHandler<TEvent>`, and the presenter pipeline takes the classes of `IPresenterHandler<TEvent>`. Thus a presenter is never a module handler.
- One presenter can show many events: it implements `IPresenterHandler<T>` one time for each event.
- An open presenter, for example `GaugePresenter<T> where T : struct, IGauge`, is closed for each `[AutoClose]` type that meets its constraints, the same as an open handler (`docs/composition.md`). If its event type does not use each type parameter (here `GaugeRead`), the presenter needs one `[AutoClose]` type that fits, else the start stops with a message (MgCohen/vsa-template#214). A presenter of an event that uses each type parameter is an open handler (`docs/composition.md`).
- `Presenter` is a service, with one forward for each presenter. Thus `GetServices<Presenter>()` in a scope makes each presenter of that scope.
- The presenter pipeline has the validators of the event, so each `Validator<TEvent>` runs one time for the event pipeline and one time for each live scope. Keep validators of events cheap and free of side effects.
- Each scope gets its own envelope. `Envelope.Services` is the provider of the presenter scope, and `Envelope.Cancellation` is the token of the publish.
- A view renders on its own thread: `PresenterWatcher` calls `InvokeAsync(StateHasChanged)`.
- The presenter pipeline runs after the event pipeline. Publish an event after the change is saved, so that a view does not show a change that a rollback removes.
- A presenter does not publish events, and does not use `IIntegrationEventPublisher` or `Pipelines` (`UMP010204`). To change state, it sends a request with `IRequestSender`, and the request handler publishes the event.
- A presenter shows the events of its own module only. The definition lets `Presenters` use the `Contracts.Events` of its own module only (`UMP020101`).

## Example

```csharp
public sealed class BellPresenter(IRequestSender requests) : Presenter(requests), IPresenterHandler<Rang> {
    public string? Last { get; private set; }

    public Task<Result<Unit>> HandleAsync(IEnvelope<Rang> envelope) {
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
