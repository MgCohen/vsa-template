# Module layout

A feature module (`Features/<Feature>/`) keeps each slice in one flat folder, `Slices/<Slice>/`, with any file names. A plugin module (`Plugins/<Module>/`) has no slices: it has one folder for each layer, as the framework has (see [Plugin module layout](#plugin-module-layout)). The folders select the layers. The app has one root project, and the rule engine checks each use against the definition (see [Projects](#projects)).

## Layout

```
Features/Notes/
├─ NotesModule.cs, NotesSettings.cs                   module root
├─ Domain/  Infrastructure/  Presenters/
└─ Slices/
   ├─ CreateNote/       CreateNoteRequest.cs, CreateNoteResponse.cs, NoteCreated.cs, CreateNoteHandler.cs
   ├─ DeleteNote/       DeleteNoteRequest.cs, DeleteNoteResponse.cs, NoteDeleted.cs, DeleteNoteHandler.cs
   ├─ ListNotes/        ListNotesRequest.cs, ListNotesResponse.cs, NoteDto.cs, ListNotesHandler.cs
   └─ LogNoteCreated/   NoteCreatedHandler.cs
```

A module with commands and a published domain (`tests/Fixtures/CommandApp`):

```
Features/Cards/
├─ CardsModule.cs                                     module root
├─ Domain/Shared/       Cost.cs
├─ Presenters/          CostLabel.cs
└─ Slices/
   └─ PayCost/          PayCostCommand.cs, PayCostResult.cs, CostPaid.cs, PayCostHandler.cs
```

- A module has only the layer folders that it needs. Ping has only `Slices/`.
- All files of one slice have one namespace, for example `Notes.Slices.CreateNote`.
- An event is in the slice that publishes it. A slice has one kind of handler: a request handler, an event handler or a command handler.
- `Slices/Shared/` holds contracts that more than one slice uses.

## Plugin module layout

A plugin module is the base of the same layer in each module, as `framework/Core` is. It has the layout of the framework: one folder for each layer, and no `Slices/`. Its root class inherits `Plugin` or `Plugin<TSettings>` (`VSA1014`), and a module root inherits `Module` or `Module<TSettings>`. `vsa add plugin <Name>` writes a new one.

```
Plugins/Pipeline/
├─ PipelinePlugin.cs                                  module root: PipelinePlugin : Plugin
├─ Contracts/
│  ├─ Requests/         Message.cs                     abstract record Message, MessageId : IResponse
│  ├─ Events/           MessageHandled.cs
│  └─ Commands/         Step.cs                        abstract record Step<T> : ICommand<T>
└─ Handlers/
   ├─ Requests/         MessageHandler.cs              abstract MessageHandler<T> : RequestHandler<T, MessageId>
   ├─ Events/Audit/     MessageAudit.cs, AuditConfig.cs
   └─ Commands/Steps/   StepHandler.cs                 abstract StepHandler<TStep, TResult> : CommandHandler<TStep, TResult>
```

The other layers (`Domain/`, `Domain/Shared/`, `Infrastructure/`, `Presenters/`) have the same folders as in a module. A module slice derives from a plugin base, for example `Heal : Step<HealResult>` and `HealHandler : StepHandler<Heal, HealResult>`.

## Projects

An app has one root project in the app root, the root of the definition. It compiles each file in its folder tree, so a module, a slice or a plugin needs no project of its own. All other projects are optional, for example a test project or a separate assembly.

```
samples/Notes/
├─ Notes.csproj                 root project: App/, Features/, Plugins/
├─ App/Host/  App/Views/
├─ Features/Notes/  Features/Ping/
└─ Tests/Notes.Tests.csproj     nested project: the root project does not compile Tests/
```

| VSA.Sdk rule | Effect |
|---|---|
| Nested project | The parent project does not compile its folder |
| `bin/`, `obj/` | Not source folders, at each depth |
| `VsaExcludedFolders` | Not source folders. Default: `Assets;Library;Temp;Logs;UserSettings;node_modules` |
| Framework references | Each project gets the base of each layer |

A web root project uses the Web SDK first, then VSA.Sdk:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <Sdk Name="VSA.Sdk" />
  <ItemGroup>
    <PackageReference Include="VSA.Host" />
    <PackageReference Include="VSA.Views" />
  </ItemGroup>
</Project>
```

A `VSA.*` package with no version gets the version of VSA.Sdk. The references do not enforce the layers: the rule engine checks each use after the build (see `architecture-engine.md`).

A module in its own assembly (for example the fixture modules in `tests/Fixtures/Host/`) has its own project in the module folder. It then references the other modules that it uses.

The host finds each class that inherits `Module` or `Plugin` in the loaded assemblies. The name of a module is the last part of the namespace of its root class, for example `Notes`. One assembly can have many modules.

`IResponse` is a marker interface. `IRequest`, `RequestHandler`, `IBus.Send` and `VsaUnitTest.Handle` require it on the response, so the compiler rejects a response without it.

## The published domain (Domain/Shared/)

`Domain/Shared/` is the published domain of a module. It is a part of its own (`Domain.Shared`), and `Domain` leaves it out. Other modules use these types in their commands, events and handlers.

```csharp
// Features/Resources/Domain/Shared/ResourceKey.cs
public sealed record ResourceKey(string Name);

// Features/Cards/Domain/Shared/Cost.cs: no type of another module
public sealed record Cost(string Resource, int Amount);
```

- Put keys, ids and data records here. Put the rules in `Domain/`, for example `Resources/Domain/Wallet.cs`.
- A `Domain.Shared` type uses only its own module. The published domain of one module does not depend on another module.
- A type holds only immutable data: `VSA1005`, `VSA1006` and `VSA1007` check it (see `architecture-rules.md`).

## Plugins and the two meanings of Shared

| Name | What it is | Who uses it | List |
|---|---|---|---|
| `Plugins/` (root folder) | Plugin modules: the base of each layer | The same layer of each module | The definition (`VsaDefinition`) |
| `Slices/Shared/` | Contracts that more than one slice uses | The slices of one module | None |
| `Domain/Shared/` | The published domain of one module | Other modules (table above) | None |

"Shared" is the name of two things in one module: `Slices/Shared/` and `Domain/Shared/`. A base of the whole app is a plugin module, not a Shared one.

The namespace of the published domain is `<Module>.Domain.Shared`. `Shared` is a VB keyword, but `VSA.globalconfig` turns off CA1716, because the code is C# only.

## Commands

A command goes from one module to another in the same process. It has one handler and one result.

| Message | Sent by | Handlers | Result | Transport |
|---|---|---|---|---|
| Request (`IRequest<T>`) | A client or a module | One `RequestHandler` | `IResponse` | In process (`IBus.Send`), or HTTP on the web host |
| Command (`ICommand<T>`) | A handler of a module | One `CommandHandler` | Any type of the slice | In process, `ICommandSender` |
| Event (`IntegrationEvent`) | A handler, after a change | Zero or more | `Unit` | `IIntegrationEventPublisher` |

Each handler takes the envelope of its message and returns `Result<T>`, and each send returns `Result<T>`. A slice can also have validators and Before or After events for its messages. See `docs/handler-pipeline.md`.

A request and a command are messages (`IMessage`). An event is not a message. Name the types `<Name>Command`, `<Name>Result` and `<Name>Handler`:

```csharp
// Features/Resources/Slices/ChangeResource/
public sealed record ChangeResourceCommand(ResourceKey Resource, int Delta) : ICommand<ChangeResourceResult>;
public sealed record ChangeResourceResult(bool Changed, int Value);

sealed class ChangeResourceHandler(Wallet wallet) : CommandHandler<ChangeResourceCommand, ChangeResourceResult> {
    public override Task<Result<ChangeResourceResult>> HandleAsync(Envelope<ChangeResourceCommand> envelope) {
        var changed = wallet.TryChange(envelope.Message.Resource, envelope.Message.Delta, out var value);
        return Task.FromResult<Result<ChangeResourceResult>>(new ChangeResourceResult(changed, value));
    }
}
```

To send a command, get `ICommandSender` from the services and call `SendAsync`:

```csharp
// Features/Cards/Slices/PayCost/PayCostHandler.cs
sealed class PayCostHandler(ICommandSender commands, IIntegrationEventPublisher events) : CommandHandler<PayCostCommand, PayCostResult> {
    public override async Task<Result<PayCostResult>> HandleAsync(Envelope<PayCostCommand> envelope) {
        var cost = envelope.Message.Cost;
        var changed = await commands.SendAsync(new ChangeResourceCommand(cost.Resource, -cost.Amount), envelope.Cancellation);
        if (!changed.IsSuccess) {
            return changed.Error!;
        }
        ...
    }
}
```

- The definition lets each handler use the `Contracts.Commands` of each module, so each handler can send each command.
- A command has exactly one handler. If two handlers handle one command, `CommandHandlerInstaller` stops start-up. If a command has no handler, `SendAsync` fails.
- `CommandHandlerInstaller` (`VSA.Handlers.Commands`) registers `ICommandSender` and each command handler of the modules (`docs/composition.md`).
- A unit test sends a command to its handler with `VsaUnitTest.Send` (see `architecture-rules.md`).

VSA has no direction check for commands. See L-04 in "Open questions".

## Open questions

- Events that more than one slice publishes: put them in `Slices/Shared/`?
- L-04: which direction can commands go between modules? No check stops two modules that send commands to each other.
