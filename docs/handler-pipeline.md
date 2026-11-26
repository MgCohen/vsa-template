# The handler pipeline

Each message (a command, a request or an event) goes through a pipeline on its way to its handler. Call sites send as before: `ICommandSender.SendAsync`, `IIntegrationEventPublisher.PublishAsync`, `IRequestSender.SendAsync`, or HTTP on the web host. The pipeline decides when the message runs, and what runs before and after the handler. An app that defines nothing gets the default pipelines: they run each message at once, with no steps.

## One run

```text
SendAsync ──▶ Envelope (id, parent, depth) ──▶ Runner: run now, or keep the envelope
                                                 │
   early steps ─▶ Validate ─▶ before steps ─▶ Before hooks ─▶ Handle ─▶ After hooks ─▶ after steps ─▶ Outcome
                                                 │
SendAsync ◀── Result<T> (the value or the Error, with its envelope)
```

| Stage | Who gives it | Rule |
|---|---|---|
| Early steps | `.BeforeStep<T>(priority)` with a priority below 0 | Lowest priority first, then the order of the builder. Same rules as a before step |
| Validate | Each validator of the validator bases of the pipeline (default `Validator<TMessage>`) | It checks the message as it is at this stage. The first `Error` rejects the message: the handler does not run |
| Before steps | `.BeforeStep<T>()` in the builder of the pipeline, priority 0 or more | Lowest priority first, then the order of the builder. It returns the message: the same one, or a copy made with `with` |
| Before hooks | Each Before hook of the modules, if the pipeline names its base | By full type name. It returns the message, or rejects it with `envelope.Reject(error)` |
| Handle | The handler of the message | One handler for a command or a request, zero or more for an event. If one event handler throws, its error goes in the result, and the next handler runs. A cancellation stops the run |
| After hooks | Each After hook of the modules, if the pipeline names its base | By full type name. Each outcome, also a fault |
| After steps | `.AfterStep<T>(priority)` in the builder | Highest priority first, then the order of the builder. Each outcome, also a fault |

Validation has the priority 0 (`Pipeline.ValidationPriority`). The default priority of a step is 0, so a step runs after validation unless its priority is lower. The validators run by full type name. A validator, a hook or a typed step for a message type also applies to each type that derives from it. `AfterCommandHook<ArenaCommand<int>, int>` sees each move, and `AfterCommandHook<StrikeCommand, int>` sees only strikes.

## Each after call runs

The After hooks and the after steps run in each run: after an accept, a reject and a fault. Each one runs also when an earlier After hook or after step throws. The first exception of the run faults the envelope and goes to the caller. The later exceptions do not.

## A step that holds a lock

A lock or a serial queue for each match scope is a pair of steps with the same low priority. The before step takes the lock before validation. The after step with the same priority runs after the other after steps, and it releases the lock.

```csharp
.BeforeStep<EnterLock>(-10)
.AfterStep<ExitLock>(-10)
```

```text
EnterLock ─▶ Validate ─▶ before steps ─▶ Before hooks ─▶ Handle ─▶ After hooks ─▶ after steps ─▶ ExitLock
```

```csharp
public sealed class EnterLock : BeforeStep {
    public override async Task<object> BeforeAsync(Envelope envelope) {
        await envelope.Services.GetRequiredService<MatchQueue>().EnterAsync();
        envelope.Items["match.lock"] = true;
        return envelope.Message;
    }
}

public sealed class ExitLock : AfterStep {
    public override Task AfterAsync(Envelope envelope) {
        if (envelope.Items.Remove("match.lock")) {
            envelope.Services.GetRequiredService<MatchQueue>().Exit();
        }
        return Task.CompletedTask;
    }
}
```

- The after step runs also when an early step threw before the lock, so it releases only a lock that the run holds (`envelope.Items`).
- A send from a handler inside the lock runs the same steps. If it goes to the same queue, the step must let the child run, for example with `envelope.Parent`.
- The pipeline gets each step from `envelope.Services` (`ActivatorUtilities.GetServiceOrCreateInstance`). Register the step type with a factory or an instance to give its values.

## Validator bases

Each pipeline runs the validators of its validator bases. With no `ValidatorsOf`, the base is `Validator<>`. A validator for an interface or a base type of the message also runs, for example `Validator<IScoreEvent>`.

| Pipeline | Validator base |
|---|---|
| Commands, events, requests, a module pipeline | `Validator<>` |
| Presenters | `PresenterValidator<>` (Umpire.Vsa.Presenters) |
| A module pipeline with `.ValidatorsOf(typeof(MyCheck<>))` | `MyCheck<>` only |

A validator base is an open generic type with one type parameter, the message, and it implements `IValidation`. Give it `[AutoRegister]`, so the scan finds its validators.

## Steps and hooks

A step belongs to one pipeline: the builder names it. A hook belongs to a module: the pipeline finds it by its base. The name of a hook ends with `Hook`, so that a reader knows that it runs before or after the handlers of each command or event of its type. `ICommand` is the command with no result type, for the constraint of `BeforeCommandHook<TCommand>`.

| Base | Project | Sees |
|---|---|---|
| `BeforeStep`, `AfterStep` | Umpire.Messages.Handlers | Each message of the pipeline, as `Envelope`. The step casts |
| `BeforeStep<T>`, `AfterStep<T>` | Umpire.Messages.Handlers | Each `T` of the pipeline, as `IEnvelope<T>` |
| `BeforeCommandHook<TCommand>` | Umpire.Vsa.Handlers.Commands | The command. It returns the command, or rejects it |
| `AfterCommandHook<TCommand, TResult>` | Umpire.Vsa.Handlers.Commands | The command and its outcome, `Result<TResult>` |
| `BeforeEventHook<TEvent>` | Umpire.Vsa.Handlers.Events | The event. It returns the event, or rejects it |
| `AfterEventHook<TEvent>` | Umpire.Vsa.Handlers.Events | The event. No outcome |

```csharp
sealed class ArmorHook : BeforeCommandHook<StrikeCommand> {
    public override Task<StrikeCommand> BeforeAsync(IEnvelope<StrikeCommand> envelope) =>
        Task.FromResult(envelope.Message with { Damage = Math.Max(0, envelope.Message.Damage - 1) });
}
```

- Put each hook in its own slice, in a module of the app. Its name ends with `Hook` (`UMP020204`).
- A hook binds to the handlers part of its target kind: `Handlers.Commands` or `Handlers.Events`.
- The constraint of each hook base takes only a command or an event, so no hook runs for a request.
- Module code uses only the four hook bases. `IBeforeCall` and `IAfterCall` are public for the hook bases. No pipeline runs a class of a module that implements them for a request.
- Two hooks have no order between them, except the type name. If the order is important, write one hook that does both parts.

## Envelope and result

The envelope is what is sent. The pipeline makes it, and each stage of the run gets it. The result is what comes back. The handler makes it, and the pipeline adds the envelope.

| Type | Project | Members |
|---|---|---|
| `Envelope`, `Envelope<TMessage>` | Umpire.Messages.Contracts | `Id`, `Parent` and `Depth` (the call chain), `State`, `Stage`, `Cancellation`, `Services`, `Items`, `Error`, `Result`, `Message`, `Original` |
| `Result<T>` | Umpire.Messages.Contracts | `IsSuccess`, `Value`, `Error`, `Envelope`. It converts from a `T` and from an `Error` |
| `Unit` | Umpire.Messages.Contracts | The value of a message with no value, for example an event |

| State | When |
|---|---|
| `Listed` | The runner keeps the envelope. It has not run yet |
| `Running` | The run started |
| `Accepted` | The handler ran. Its result can still have an `Error` |
| `Rejected` | A validator, a before step or a Before hook rejected the message. The handler did not run |
| `Faulted` | An exception. The After hooks and the after steps run, then the exception goes to the caller |

Each envelope gets the next `Id` of one counter of the process. A send in a handler, a hook or a step gets the running envelope as its `Parent`, also when it goes to another pipeline. Domain code does not see results or envelopes.

## Handlers

Each handler takes the envelope and returns `Result<T>`:

```csharp
sealed class StrikeHandler(ArenaLog log) : CommandHandler<StrikeCommand, int> {
    public override Task<Result<int>> HandleAsync(IEnvelope<StrikeCommand> envelope) =>
        Task.FromResult<Result<int>>(envelope.Message.Damage);
}

sealed class RangHandler(RingLog log) : IntegrationEventHandler<Rang> {
    public override Task<Result<Unit>> HandleAsync(IEnvelope<Rang> envelope) {
        log.Add(envelope.Message.Bell);
        return Done;
    }
}
```

### How the pipeline calls a handler

`IHandler<TMessage>` is a marker with no members. The pipeline finds the handlers by it. The pipeline gives the call: each pipeline has a list of handler calls, one for each kind of handler. No handler interface has a default method.

| Handler kind | Call | How the call is given |
|---|---|---|
| `CommandHandler<,>`, `RequestHandler<,>`, `IntegrationEventHandler<>` | `RunCall<>`: `IRunnableHandler<T>.RunAsync` | The default of each pipeline |
| `IIntegrationEventHandler<>` | `EventHandlerCall<>`: `HandleAsync` | `.CallsEventHandlers()` |
| `IPresenterHandler<>` | `EventHandlerCall<>`, then `Changed` | `.CallsPresenters()` |

```csharp
public sealed class EventHandlerCall<TEvent> : HandlerCall<IIntegrationEventHandler<TEvent>, TEvent>
    where TEvent : IntegrationEvent {
    public override async Task<IMessageResult> CallAsync(
        IIntegrationEventHandler<TEvent> handler, IEnvelope<TEvent> envelope) =>
        await handler.HandleAsync(envelope);
}
```

- A handler call is an open generic type with one type parameter, the message. It derives from `HandlerCall<THandler, TMessage>`.
- For each handler of a route, the pipeline closes each call with the message type of the handler, one time. The first call whose `THandler` the handler implements wins. `RunCall<>` is always the last call.
- A call that does not meet its constraints for a message is not used, for example `EventHandlerCall<IStamped>` for a message that is not an `IntegrationEvent`.
- If no call fits a handler, the call of that handler fails in the run, with an error that names the handler and `Calls(typeof(...))`. In an event run, the next handlers run.
- A module pipeline of interface handlers names their call: `.CallsEventHandlers()` for an `IIntegrationEventHandler<>`, `.CallsPresenters()` for a presenter. With `.CallsEventHandlers()` only, a presenter does not raise `Changed`.
- `.AfterHandled<THandler>(action)` runs the action on each handler instance of type `THandler` after its call returns a result, also an error result. A throw skips the action. The presenter pipeline raises `Changed` with it.

The call casts the handler to the closed handler interface of its message type. Thus one class can handle many message types, for example a presenter:

```csharp
public sealed class CardPresenter(IRequestSender r) : Presenter(r),
    IPresenterHandler<CardAdded>, IPresenterHandler<CardRemoved> { ... }   // one HandleAsync for each event
```

### Which handler runs

A handler of a base message type also handles each derived message. The pipeline uses one match for all stages: a part for `B` applies to each message that is a `B` (`MessageTypes.Applies`).

| Kind | Handlers that run |
|---|---|
| Command, request | The handler of the nearest type: the exact type first, then the nearest base type or interface. If two handlers have the same distance, the send fails, and the error names both |
| Event | Each handler of the event type, of each base type and of each interface, one time each. The nearest type comes first. Types at the same distance run in the order of their full names |

The check for two handlers at the same distance runs at the first send of each message type, not at start. The pipelines are made at the first send, and a derived message type can come from any module, so the start does not know each message type. The send of that message type fails each time, with the same error.

```csharp
public abstract record JsCommand<TResult> : JsCommand, ICommand<TResult>;
public sealed record MotionWait(string Target) : JsCommand<WaitResult>;
public sealed record Frames(int Count) : JsCommand<int>;

public sealed class JsCommandHandler<TResult>(IJSRuntime js) : CommandHandler<JsCommand<TResult>, TResult> {
    public override Task<Result<TResult>> HandleAsync(IEnvelope<JsCommand<TResult>> envelope) => ...;
}
```

The message also closes an open handler. For `MotionWait`, the pipeline reads `WaitResult` from the base chain and closes `JsCommandHandler<WaitResult>`, with a check of the constraints. Open validators and open hooks are closed the same way. The route of each message type is made one time. The open class can have more than one generic parameter, and its message type can nest them, for example `PutHandler<T> : CommandHandler<Put<T>, int>`. `OpenGenerics.Unify` solves the parameters and checks the constraints. If a type parameter of the handler is not in its message type, the start stops. An open handler in `Handlers(typeof(...))` gets the same check when its pipeline is made. A message can implement one generic interface two times, for example `IWrap<int>` and `IWrap<string>`. Then an open handler of `IWrap<T>` closes for each one: an event runs each closure, and a command fails with the error of two handlers at the same distance. If a sent message does not meet a constraint of the open handler, the error names the handler, the message and the constraint. In Unity (IL2CPP), the `[AutoClose]` types make the closed types exist at build time (`docs/composition.md`).

## Runners

| Runner | Behavior |
|---|---|
| `InlineRunner` | The default. Each send runs at once. A send from a hook or a step runs inside the run of its parent |
| A custom `IRunner` | `RunAsync(envelope, run)` decides when `run` starts, for example in a `try` block that records a fault (the `StepRunner` of the engine) |

The runner of the pipeline of the message decides when a send runs. A `SendAsync` task completes when its own message has run. Runners that keep a message in a queue or a stack until a later start (for example one time in each frame of a game loop) belong to the `vsa-unity` package.

## Define a pipeline

A pipeline names types only: the message base, the handlers, the hook bases, the validator bases, the runner and the steps.

```csharp
public sealed class ArenaModule : Module {
    protected override void Configure(IServiceCollection services) {
        services.AddSingleton<ArenaLog>();
        services.AddPipeline(new PipelineBuilder<ArenaMove>()
            .HandlersOf(typeof(CommandHandler<,>))
            .HooksOf(typeof(BeforeCommandHook<>), typeof(AfterCommandHook<,>))
            .BeforeStep<TagStep>()
            .AfterStep<AuditStep>());
    }
}
```

| Member | Use |
|---|---|
| `new PipelineBuilder<TMessage>()` | A description of a pipeline for `TMessage` and each type that derives from it |
| `.Handlers<TBase>()`, `.HandlersOf(typeof(TBase<,>))` | Each handler of the modules that derives from the base |
| `.Handlers(typeof(A), typeof(B))` | Only these handlers |
| `.HooksOf(typeof(A<>), typeof(B<,>))` | Each hook of the modules that derives from these bases. A step base is not a hook base. With no `HooksOf`, no hook runs |
| `.Calls(typeof(A<>))` | The handler calls of this pipeline, before the default `RunCall<>` |
| `.AfterHandled<THandler>(action)` | An action on each `THandler` instance after its call returns a result |
| `.Runner<TRunner>()` | The runner. Default: `InlineRunner` |
| `.ValidatorsOf(typeof(A<>))` | The validator bases of this pipeline. Default: `Validator<>` |
| `.BeforeStep<T>(priority)`, `.AfterStep<T>(priority)` | A step of this pipeline. `T` derives from `BeforeStep` or `AfterStep`. Default priority 0. Add each step one time |
| `.Build(services)` | A new `Pipeline` with its own runner. `pipeline.SendAsync<TResult>(message)` runs a message of `TMessage` on it, and a message of another type fails. Keep the builder to build more instances, for example one for each match |
| `services.AddPipeline(builder)` | The app instance: `ICommandSender`, `IRequestSender` and the publisher send each message of `TMessage` to it, if the handler bases of the builder select it |

The handler bases select the pipeline. Each sender gives the handler base of its kind, for example `typeof(CommandHandler<,>)`. A send goes to the pipeline that:

1. Has a message base that matches the message.
2. Selects the handler base of the sender: a base in `HandlersOf` or a listed handler is that base or derives from it. For example, `HandlersOf(typeof(FinisherHandler<>))` takes the sends of `ICommandSender` if `FinisherHandler<T>` derives from `CommandHandler<T, int>`.
3. Has a handler for the message. An event pipeline always runs the event. If no pipeline has a handler, the send goes to a pipeline that matches only items 1 and 2.

If more than one pipeline is left, the send goes to the pipeline with the most specific message base. If the bases do not derive from each other, the send goes to the pipeline that was added last. A module adds its pipelines after the defaults, so a module pipeline wins.

Each kind adds its default pipeline with `AddPipeline`, as a module does:

| Default pipeline | Handlers | Hook bases |
|---|---|---|
| Commands | `CommandHandler<,>` | `BeforeCommandHook<>`, `AfterCommandHook<,>` |
| Events | `IntegrationEventHandler<>`, `.CallsEventHandlers()` | `BeforeEventHook<>`, `AfterEventHook<>` |
| Presenters | `IPresenterHandler<>`, `.CallsPresenters()`, runner `LiveScopesRunner`, validators `PresenterValidator<>` | None |
| Requests | `RequestHandler<,>` | None |

A message base has one pipeline for each handler base: a second `AddPipeline` with the same message base and a related handler base fails. Two handler bases are related if one derives from the other. A listed handler counts as a handler base. The command and the request defaults both have the base `IMessage`, with different handler bases.

The default of each kind already has its message base and handler base. Thus a module pipeline with `PipelineBuilder<IMessage>().HandlersOf(typeof(CommandHandler<,>))` fails, and it does not replace the default. Give a module pipeline a narrower message base, for example `ArenaMove`.

### Broadcast: an event goes to each pipeline

A command or a request goes to one pipeline. An event (`IBroadcastMessage`) goes to each pipeline that takes it:

```text
IIntegrationEventPublisher.PublishAsync(event)
 └─ Pipelines.PublishAsync
     1. events      IntegrationEventHandler<>   in the scope of the publish     → its result is the result of the publish
     2. presenters  IPresenterHandler<>         in each live presenter scope    → only if 1 is Accepted
     3. a module broadcast pipeline, for example an audit                       → only if 1 is Accepted
```

| Rule | Detail |
|---|---|
| Pipelines | Each pipeline with a broadcast message base that takes the event |
| One for each handler base | If pipelines have related handler bases, the most specific message base wins |
| Order | The order of `AddPipeline`. A group of related pipelines has the place of its first pipeline |
| Gate | The later pipelines run only if the first one is `Accepted` |

## Projects

| Project | Types |
|---|---|
| Umpire.Messages.Contracts | `IMessage`, `IBroadcastMessage`, `IEnvelope`, `IEnvelope<T>`, `Envelope`, `EnvelopeState`, `EnvelopeStage`, `Result<T>`, `Error`, `Unit`, `IMessageResult` |
| Umpire.Messages.Handlers | `PipelineBuilder<T>`, `Pipeline`, `Pipelines`, `IHandler<T>`, `IRunnableHandler<T>`, `HandlerCall<THandler, TMessage>`, `IRunner`, `InlineRunner`, `Validator<T>`, `IValidation`, `BeforeStep`, `AfterStep` and their typed forms, `IBeforeCall`, `IAfterCall`, `AddPipeline` |
| Umpire.Vsa.Handlers.Commands, .Events, .Requests | The handler bases, the hook bases, the senders, and the default pipeline of each kind |

The folders of Umpire.Messages.Handlers are `Pipeline/`, `Runners/`, `Stages/` and `Envelopes/`. The namespace of each file is `Umpire.Messages.Handlers`.

`AddPipeline` registers the pipeline and `Pipelines`, the one service that selects a pipeline for each send. Each pipeline reads `AppTypes` from the services: it finds its handlers, the validators of its validator bases and the hooks of its hook bases. A pipeline built from services with no `AppTypes` takes only listed handlers, and `HandlersOf` fails. All these projects also target `netstandard2.1`, for Unity.

Later, on the same envelope: typed metadata (`Envelope<TMessage, TMeta>`), limits on depth and loops, queries over the call chain, subscriptions at run time, and runs that suspend and resume.
