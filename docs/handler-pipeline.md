# The handler pipeline

Each message (a command, a request or an event) goes through a pipeline on its way to its handler. Call sites send as before: `ICommandSender.SendAsync`, `IIntegrationEventPublisher.PublishAsync`, `IBus.Send`, or HTTP on the web host. The pipeline decides when the message runs, and what runs before and after the handler. An app that defines nothing gets the default pipelines: they run each message at once, with no steps.

## One run

```text
SendAsync ──▶ Envelope (id, parent, depth) ──▶ Runner: run now, or keep in a queue or a stack
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
| Handle | The handler of the message | One handler for a command or a request, zero or more for an event |
| After hooks | Each After hook of the modules, if the pipeline names its base | By full type name. Each outcome, also a fault |
| After steps | `.AfterStep<T>()` in the builder | In the order of the builder. Each outcome, also a fault |

The validators run by full type name. A validator, a hook or a typed step for a message type also applies to each type that derives from it. `AfterCommandHook<ArenaCommand<int>, int>` sees each move, and `AfterCommandHook<StrikeCommand, int>` sees only strikes.

## Steps and hooks

A step belongs to one pipeline: the builder names it. A hook belongs to a module: the pipeline finds it by its base.

| Base | Project | Sees |
|---|---|---|
| `BeforeStep`, `AfterStep` | VSA.Handlers | Each message of the pipeline, as `Envelope`. The step casts |
| `BeforeStep<T>`, `AfterStep<T>` | VSA.Handlers | Each `T` of the pipeline, as `IEnvelope<T>` |
| `BeforeCommandHook<TCommand>` | VSA.Handlers.Commands | The command. It returns the command, or rejects it |
| `AfterCommandHook<TCommand, TResult>` | VSA.Handlers.Commands | The command and its outcome, `Result<TResult>` |
| `BeforeEventHook<TEvent>` | VSA.Handlers.Events | The event. It returns the event, or rejects it |
| `AfterEventHook<TEvent>` | VSA.Handlers.Events | The event. No outcome |

```csharp
sealed class ArmorHook : BeforeCommandHook<StrikeCommand> {
    public override Task<StrikeCommand> BeforeAsync(IEnvelope<StrikeCommand> envelope) =>
        Task.FromResult(envelope.Message with { Damage = Math.Max(0, envelope.Message.Damage - 1) });
}
```

- Put each hook in its own slice, in a module of the app. Its name ends with `Hook` (`VSA1015`).
- A hook binds to the handlers part of its target kind: `Handlers.Commands` or `Handlers.Events`.
- The constraint of each hook base takes only a command or an event, so no hook runs for a request.
- Module code uses only the four hook bases. `IBeforeCall` and `IAfterCall` are public for the hook bases. No pipeline runs a class of a module that implements them for a request.
- Two hooks have no order between them, except the type name. If the order is important, write one hook that does both parts.

## Envelope and result

The envelope is what is sent. The pipeline makes it, and each stage of the run gets it. The result is what comes back. The handler makes it, and the pipeline adds the envelope.

| Type | Project | Members |
|---|---|---|
| `Envelope`, `Envelope<TMessage>` | VSA.Contracts | `Id`, `Parent` and `Depth` (the call chain), `State`, `Stage`, `Cancellation`, `Services`, `Items`, `Error`, `Result`, `Message`, `Original` |
| `Result<T>` | VSA.Contracts | `IsSuccess`, `Value`, `Error`, `Envelope`. It converts from a `T` and from an `Error` |
| `Unit` | VSA.Contracts | The value of a message with no value, for example an event |

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

A handler base of a plugin module implements `IHandler<TMessage>` one time, as `CommandHandler<,>`, `RequestHandler<,>` and `IntegrationEventHandler<>` do.

## Runners

| Runner | Behavior |
|---|---|
| `InlineRunner` | The default. Each send runs at once |
| `QueuedRunner` | Each send waits in a queue. `pipeline.StartAsync()` runs the queue until it is empty, for example one time in each frame of a game loop |
| `StackRunner` | The first send runs, then each message that is sent during the run, until the queue is empty. A send from a validator, a before stage or a handler runs at once. A send from an After hook or an after step goes on a stack and runs before the queue, newest first |

The runner of the pipeline of the message decides where a send goes. A `SendAsync` task completes when its own message has run. With `StackRunner`, a handler or a hook must not await a message that it sends to the same pipeline and that the runner keeps: that message runs after the current one. Do not await it (`_ = sender.SendAsync(...)`), or send it to an inline pipeline.

## Define a pipeline

A pipeline names types only: the message base, the handlers, the hook bases, the runner and the steps.

```csharp
public sealed class ArenaModule : Module {
    public override void Configure(IServiceCollection services) {
        services.AddSingleton<ArenaLog>();
        services.AddPipeline(new PipelineBuilder<ArenaMove>()
            .HandlersOf(typeof(CommandHandler<,>))
            .HooksOf(typeof(BeforeCommandHook<>), typeof(AfterCommandHook<,>))
            .Runner<StackRunner>()
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
| `.HooksOf(typeof(A<>), typeof(B<,>))` | Each hook of the modules that derives from these bases. With no `HooksOf`, no hook runs |
| `.Runner<TRunner>()` | The runner. Default: `InlineRunner` |
| `.BeforeStep<T>()`, `.AfterStep<T>()` | A step of this pipeline. `T` derives from `BeforeStep` or `AfterStep` |
| `.Build(services)` | A new instance with its own runner. Keep the builder to build more instances, for example one for each match |
| `services.AddPipeline(builder)` | The module instance: `ICommandSender`, `IBus` and the publisher send each message of `TMessage` to it, and the services give it as `Pipeline<TMessage>` |

A send goes to the most specific pipeline whose message base matches the message and that has a handler for it (an event pipeline always runs the event). If none matches, it goes to the default pipeline of its kind. The key of a default pipeline is its handler base, for example `typeof(CommandHandler<,>)`.

| Default pipeline | Handlers | Hook bases |
|---|---|---|
| Commands | `CommandHandler<,>` | `BeforeCommandHook<>`, `AfterCommandHook<,>` |
| Events | `IntegrationEventHandler<>` | `BeforeEventHook<>`, `AfterEventHook<>` |
| Requests | `RequestHandler<,>` | None | A message base has one pipeline: a second `AddPipeline` for the same base fails.

## Projects

| Project | Types |
|---|---|
| VSA.Contracts | `Envelope`, `Result<T>`, `Error`, `Unit`, `IMessageResult` |
| VSA.Handlers | `PipelineBuilder<T>`, `Pipeline<T>`, `Pipelines`, `IHandler<T>`, `IRunner` and the runners, `Validator<T>`, `BeforeStep`, `AfterStep` and their typed forms, `IBeforeCall`, `IAfterCall`, `AddPipelines` |
| VSA.Handlers.Commands, .Events, .Requests | The handler bases, the hook bases, the senders, and the default pipeline of each kind |

The folders of VSA.Handlers are `Pipeline/`, `Runners/`, `Stages/` and `Envelopes/`. The namespace of each file is `VSA.Handlers`.

`VsaHost.Install` calls `AddPipelines` before the handler registrations. It finds the validators of the modules and registers `Pipelines`. Each pipeline finds the hooks of its hook bases. All these projects also target `netstandard2.1`, for Unity.

Later, on the same envelope: typed metadata (`Envelope<TMessage, TMeta>`), limits on depth and loops, queries over the call chain, subscriptions at run time, and runs that suspend and resume.
