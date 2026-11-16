# Module layout

A feature module (`Features/<Feature>/`) keeps each slice in one flat folder, `Slices/<Slice>/`, with any file names. A plugin module (`Plugins/<Module>/`) has no slices: it has one folder for each layer, as the framework has (see [Plugin module layout](#plugin-module-layout)). The folders select the layers. The app has one root project, and the rule engine checks each use against the definition (see [Projects](#projects)).

## Layout

```
Features/Notebook/
├─ NotebookModule.cs, NotebookSettings.cs             module root
├─ Domain/  Infrastructure/  Presenters/
└─ Slices/
   ├─ CreateNote/       CreateNoteRequest.cs, CreateNoteResponse.cs, NoteCreated.cs, CreateNoteHandler.cs
   ├─ DeleteNote/       DeleteNoteRequest.cs, DeleteNoteResponse.cs, NoteDeleted.cs, DeleteNoteHandler.cs
   ├─ ListNotes/        ListNotesRequest.cs, ListNotesResponse.cs, NoteDto.cs, ListNotesHandler.cs
   └─ LogNoteCreated/   NoteCreatedHandler.cs
```

A module with commands and a published domain (the modules `Battle`, `Cards` and `Resources` in `tests/Fixtures/Modules/Features/`):

```
Features/Cards/
├─ CardsModule.cs                                     module root
├─ Domain/Shared/       Cost.cs
├─ Presenters/          CostLabel.cs
└─ Slices/
   └─ PayCost/          PayCostCommand.cs, PayCostResult.cs, CostPaid.cs, PayCostHandler.cs
```

- A module has only the layer folders that it needs. Ping has only `Slices/`.
- All files of one slice have one namespace, for example `Notes.Features.Notebook.Slices.CreateNote`. Each namespace starts with the app name (the name of the root project in the block folder) and follows the folders. This is a guideline: no check reads the namespace.
- An event is in the slice that publishes it. A slice has one kind of handler: a request handler, an event handler or a command handler.
- `Slices/Shared/` holds contracts that more than one slice uses. It is the part `Slice.Shared`, not a slice: the slices and the presenters of its own feature use it, and the slice counts do not count it.

## Plugin module layout

A plugin module is the base of the same layer in each module, as the VSA pack (`packs/Vsa`) is. It has the layout of the framework: one folder for each layer, and no `Slices/`. Its root class inherits `Plugin` or `Plugin<TSettings>` (`UMP020101`, `UMP020301`), and a module root inherits `Module` or `Module<TSettings>`. `umpire add plugin <Name>` writes a new one.

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

An app has one root project in the block folder. It compiles each file in its folder tree, so a module, a slice or a plugin needs no project of its own. All other projects are optional, for example a test project or a separate assembly.

```
tests/Fixtures/Notes/
├─ Notes.csproj                 root project: App/, Features/, Plugins/
├─ App/Host/  App/Views/
├─ Features/Notebook/  Features/Ping/
└─ Tests/Notes.Tests.csproj     nested project: the root project does not compile Tests/
```

| Umpire.Sdk rule | Effect |
|---|---|
| Nested project | The parent project does not compile its folder |
| `bin/`, `obj/` | Not source folders, at each depth |
| `UmpireExcludedFolders` | Not source folders. Compile scope only: the definition and the engine do not read it. Default: `Assets;Library;Temp;Logs;UserSettings;node_modules` |
| Framework references | Each project gets the base of each layer |

A web root project uses the Web SDK first, then Umpire.Sdk:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <Sdk Name="Umpire.Sdk" />
  <ItemGroup>
    <PackageReference Include="Umpire.Vsa.Hosts.Web" />
    <PackageReference Include="Umpire.Vsa.Views" />
  </ItemGroup>
</Project>
```

A `Umpire.*` package that Umpire.Sdk does not add, for example `Umpire.Vsa.Hosts.Web`, gives its version: `Version`, or a `PackageVersion` with central package management. Use the version of Umpire.Sdk: the rule engine checks it (`UMP030201`). The references do not enforce the layers: the rule engine checks each use after the build (see `architecture-engine.md`).

A module in its own assembly (for example the fixture modules in `tests/Fixtures/Modules/`) has its own project in the module folder. It then references the other modules that it uses.

The host finds each class that inherits `Module` or `Plugin` in the loaded assemblies. The name of a module is the last part of the namespace of its root class, for example `Notebook`. One assembly can have many modules.

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
- A type holds only immutable data. The rules `UMP010205`, `UMP010206` and `UMP010207` are disabled until the review in #143 (see "Disabled rules" in `architecture-rules.md`).

## Plugins and the two meanings of Shared

| Name | What it is | Who uses it | List |
|---|---|---|---|
| `Plugins/` (root folder) | Plugin modules: the base of each layer | The same layer of each module | The definition (`VsaArchitecture`) |
| `Slices/Shared/` | Contracts that more than one slice uses | The slices of one module | None |
| `Domain/Shared/` | The published domain of one module | Other modules (table above) | None |

"Shared" is the name of two things in one module: `Slices/Shared/` and `Domain/Shared/`. A base of the whole app is a plugin module, not a Shared one.

The namespace of the published domain is `<App>.Features.<Module>.Domain.Shared`. `Shared` is a VB keyword, but `Umpire.globalconfig` turns off CA1716, because the code is C# only.

## Commands

A command goes from one module to another in the same process. It has one handler and one result.

| Message | Sent by | Handlers | Result | Transport |
|---|---|---|---|---|
| Request (`IRequest<T>`) | A client or a module | One `RequestHandler` | `IResponse` | In process (`IBus.Send`), or HTTP on the web host |
| Command (`ICommand<T>`) | A handler of a module | One `CommandHandler` | Any type of the slice | In process, `ICommandSender` |
| Event (`IntegrationEvent`) | A handler, after a change | Zero or more | `Unit` | `IIntegrationEventPublisher` |

Each handler takes the envelope of its message and returns `Result<T>`, and each send returns `Result<T>`. A slice can also have validators for its messages. A Before or After hook is in its own slice: it is the part `Handlers.Commands.Hooks` or `Handlers.Events.Hooks`, and the engine checks that a slice has one kind of handler (`UMP020301`). See `docs/handler-pipeline.md`.

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
- A command has exactly one handler. If two handlers handle one command, `AddCommandHandlers` stops start-up. If a command has no handler, `SendAsync` fails.
- `AddCommandHandlers` (`Umpire.Vsa.Handlers.Commands`) registers `ICommandSender` and each command handler of the modules (`docs/composition.md`).
- A unit test sends a command to its handler with `VsaUnitTest.Send` (see `architecture-rules.md`).

VSA has no direction check for commands. See L-04 in "Open questions".

## Open questions

- Events that more than one slice publishes: put them in `Slices/Shared/`?
- L-04: which direction can commands go between modules? No check stops two modules that send commands to each other.
