# The handler pipeline

Each message (a command, a request or an event) goes through a pipeline on its way to its handler. Call sites send as before: `ICommandSender.SendAsync`, `IIntegrationEventPublisher.PublishAsync`, `IBus.Send`, or HTTP on the web host. The pipeline decides when the message runs, and what runs before and after the handler. An app that defines nothing gets the default pipelines: they run each message at once, with no steps.

## One run

```text
SendAsync ──▶ Envelope (id, parent, depth) ──▶ Runner: run now, or keep in a queue or a stack
                                                 │
   Validate ─▶ before steps ─▶ Before events ─▶ Handle ─▶ After events ─▶ after steps ─▶ Outcome
                                                 │
SendAsync ◀── Result<T> (the value or the Error, with its envelope)
```

| Stage | Who gives it | Rule |
|---|---|---|
| Validate | Each `Validator<TMessage>` of the modules | It checks the message as it is at this stage. The first `Error` rejects the message: the handler does not run |
| Before steps | `.BeforeStep<T>()` in the builder of the pipeline | In the order of the builder. It returns the message: the same one, or a copy made with `with` |
| Before events | Each `Before<TMessage>` of the modules | By `Order`, then by type name. It returns the message, or rejects it with `envelope.Reject(error)` |
| Handle | The handler of the message | One handler for a command or a request, zero or more for an event |
| After events | Each `After<TMessage>` of the modules | By `Order`, then by type name. Each outcome, also a fault |
| After steps | `.AfterStep<T>()` in the builder | In the order of the builder. Each outcome, also a fault |

A validator, an event or a step for a message type also applies to each type that derives from it. `After<ArenaMove>` sees each move, and `After<StrikeCommand>` sees only strikes.

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
| `Rejected` | A validator or a Before event rejected the message. The handler did not run |
| `Faulted` | An exception. The After events and the after steps run, then the exception goes to the caller |

A send in a handler, an event or a step gets the running envelope as its `Parent`, also when it goes to another pipeline. Domain code does not see results or envelopes.

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
| `QueuedRunner` | Each send waits in a queue. `pipeline.StartAsync()` runs the queue until it is empty |
| `DrainRunner` | The first send runs, then each message that is sent during the run, first in first out, until the queue is empty |
| `StackRunner` | As `DrainRunner`. A send from a validator, a before stage or a handler runs at once. A send from an After event or an after step goes on a stack and runs before the queue, newest first |

The runner of the pipeline of the message decides where a send goes. A `SendAsync` task completes when its own message has run. With `DrainRunner` and `StackRunner`, a handler or an event must not await a message that it sends to the same pipeline and that the runner keeps: that message runs after the current one. Do not await it (`_ = sender.SendAsync(...)`), or send it to an inline pipeline.

## Define a pipeline

A pipeline names types only: the message base, the handlers, the runner and the steps.

```csharp
public sealed class ArenaModule : Module {
    public override void Configure(ModuleBuilder module) {
        module.Services.AddSingleton<ArenaLog>();
        module.Services.AddPipeline(new PipelineBuilder<ArenaMove>()
            .HandlersOf(typeof(CommandHandler<,>))
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
| `.Runner<TRunner>()` | The runner. Default: `InlineRunner` |
| `.BeforeStep<T>()`, `.AfterStep<T>()` | A step for each message of this pipeline |
| `.Build(services)` | A new instance with its own runner. Keep the builder to build more instances, for example one for each match |
| `services.AddPipeline(builder)` | The module instance: `ICommandSender`, `IBus` and the publisher send each message of `TMessage` to it, and the services give it as `IPipeline<TMessage>` |

A send goes to the most specific pipeline whose message base matches the message. If none matches, it goes to the default pipeline of its kind. A message base has one pipeline: a second `AddPipeline` for the same base fails.

## Projects

| Project | Types |
|---|---|
| VSA.Contracts | `Envelope`, `Result<T>`, `Error`, `Unit`, `IMessageResult` |
| VSA.Handlers | `PipelineBuilder<T>`, `IPipeline<T>`, `Pipelines`, `IHandler<T>`, `IRunner` and the runners, `Validator<T>`, `Before<T>`, `After<T>`, `BeforeStep<T>`, `AfterStep<T>`, `IMessageIds`, `PipelineInstaller` |
| VSA.Handlers.Commands, .Events, .Requests | The handler bases, the senders, and the default pipeline of each kind |

`PipelineInstaller` runs before the handler installers. It finds the validators and the Before and After events of the modules, and registers `Pipelines` and `IMessageIds`. All these projects also target `netstandard2.1`, for Unity.

Later, on the same envelope: typed metadata (`Envelope<TMessage, TMeta>`), limits on depth and loops, queries over the call chain, subscriptions at run time, and runs that suspend and resume.
