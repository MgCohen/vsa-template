# The handler pipeline

Each message (a command, a request or an event) goes through a pipeline on its way to its handler. Call sites send as before: `ICommandSender.SendAsync`, `IIntegrationEventPublisher.PublishAsync`, `IRequestSender.SendAsync`, or HTTP on the web host. The pipeline decides when the message runs, and what runs before and after the handler. An app that defines nothing gets the default pipelines: they run each message at once, with no steps.

## One run

```text
SendAsync ──▶ Envelope (id, parent, depth) ──▶ Runner: run now, or keep the envelope
                                                 │
   Validate ─▶ before steps ─▶ Before hooks ─▶ Handle ─▶ After hooks ─▶ after steps ─▶ Outcome
                                                 │
SendAsync ◀── Result<T> (the value or the Error, with its envelope)
```

| Stage | Who gives it | Rule |
|---|---|---|
| Validate | Each `Validator<TMessage>` of the modules | It checks the message as it is at this stage. The first `Error` rejects the message: the handler does not run |
| Before steps | `.BeforeStep<T>()` in the builder of the pipeline | In the order of the builder. It returns the message: the same one, or a copy made with `with` |
| Before hooks | Each Before hook of the modules, if the pipeline names its base | By full type name. It returns the message, or rejects it with `envelope.Reject(error)` |
| Handle | The handler of the message | One handler for a command or a request, zero or more for an event. If one event handler throws, its error goes in the result, and the next handler runs. A cancellation stops the run |
| After hooks | Each After hook of the modules, if the pipeline names its base | By full type name. Each outcome, also a fault |
| After steps | `.AfterStep<T>()` in the builder | In the order of the builder. Each outcome, also a fault |

The validators run by full type name. A validator, a hook or a typed step for a message type also applies to each type that derives from it. `AfterCommandHook<ArenaCommand<int>, int>` sees each move, and `AfterCommandHook<StrikeCommand, int>` sees only strikes.

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

- Put each hook in its own slice, in a module of the app. Its name ends with `Hook` (`UMP010209`).
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
    public override Task<Result<int>> HandleAsync(Envelope<StrikeCommand> envelope) =>
        Task.FromResult<Result<int>>(envelope.Message.Damage);
}

sealed class RangHandler(RingLog log) : IntegrationEventHandler<Rang> {
    public override Task<Result<Unit>> HandleAsync(Envelope<Rang> envelope) {
        log.Add(envelope.Message.Bell);
        return Done;
    }
}
```

The pipeline calls `IHandler<TMessage>.RunAsync(Envelope<TMessage>)`. Each handler base gives that body one time, as `CommandHandler<,>`, `RequestHandler<,>` and `IIntegrationEventHandler<>` do. Each closed `IHandler<T>` has its own `RunAsync`, so one class can handle many message types, for example a presenter:

```csharp
public sealed class CardPresenter(IRequestSender r) : Presenter(r),
    IPresenterHandler<CardAdded>, IPresenterHandler<CardRemoved> { ... }   // one HandleAsync for each event
```

## Runners

| Runner | Behavior |
|---|---|
| `InlineRunner` | The default. Each send runs at once. A send from a hook or a step runs inside the run of its parent |
| A custom `IRunner` | `RunAsync(envelope, run)` decides when `run` starts, for example in a `try` block that records a fault (the `StepRunner` of the engine) |

The runner of the pipeline of the message decides when a send runs. A `SendAsync` task completes when its own message has run. Runners that keep a message in a queue or a stack until a later start (for example one time in each frame of a game loop) belong to the `vsa-unity` package.

## Define a pipeline

A pipeline names types only: the message base, the handlers, the hook bases, the runner and the steps.

```csharp
public sealed class ArenaModule : Module {
    public override void Configure(IServiceCollection services) {
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
| `.Runner<TRunner>()` | The runner. Default: `InlineRunner` |
| `.BeforeStep<T>()`, `.AfterStep<T>()` | A step of this pipeline. `T` derives from `BeforeStep` or `AfterStep`. Add each step one time |
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
| Events | `IntegrationEventHandler<>` | `BeforeEventHook<>`, `AfterEventHook<>` |
| Presenters | `IPresenterHandler<>`, runner `LiveScopesRunner` | None |
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
| Umpire.Messages.Handlers | `PipelineBuilder<T>`, `Pipeline`, `Pipelines`, `IHandler<T>`, `IRunner`, `InlineRunner`, `Validator<T>`, `BeforeStep`, `AfterStep` and their typed forms, `IBeforeCall`, `IAfterCall`, `AddPipeline` |
| Umpire.Vsa.Handlers.Commands, .Events, .Requests | The handler bases, the hook bases, the senders, and the default pipeline of each kind |

The folders of Umpire.Messages.Handlers are `Pipeline/`, `Runners/`, `Stages/` and `Envelopes/`. The namespace of each file is `Umpire.Messages.Handlers`.

`AddPipeline` registers the pipeline and `Pipelines`, the one service that selects a pipeline for each send. Each pipeline reads `AppTypes` from the services: it finds its handlers, the validators and the hooks of its hook bases. A pipeline built from services with no `AppTypes` takes only listed handlers, and `HandlersOf` fails. All these projects also target `netstandard2.1`, for Unity.

Later, on the same envelope: typed metadata (`Envelope<TMessage, TMeta>`), limits on depth and loops, queries over the call chain, subscriptions at run time, and runs that suspend and resume.
