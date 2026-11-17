# Vocabulary

Each term has one meaning in the code, the docs and the messages. Umpire is the product, and VSA is one architecture pack of Umpire.

To make a first app and fix a first rule error, see [getting-started.md](getting-started.md).

A code name with the mark "(D6)" comes with the sender rename of the D6 work package. The old name is in the same row.

## Product and packs

| Term | Meaning | Code names |
|---|---|---|
| Umpire | The product: framework, rule engine, tool and SDK | `Umpire.Kernel`, `umpire` tool |
| Framework | The Umpire code in `framework/`: the framework kernel and the components. Not a .NET assembly, and not an external part | `framework/Kernel/`, `framework/Components/` |
| Pack | One architecture with its definition, bases and rules | `packs/Vsa/`, `Vsa<THost>` |
| Pack variant | A pack whose architecture inherits the architecture of another pack and adds parts and rules | `Packs/Ecs/`, `EcsArchitecture : VsaArchitecture` |
| VSA | The Vertical Slice Architecture pack | `Umpire.Vsa.*`, `VsaArchitecture` |
| Architecture | A definition: concepts, parts and rules | `VsaArchitecture : Architecture` |

## Apps and hosts

| Term | Meaning | Code names |
|---|---|---|
| App | A block that runs. Its app class is the check entry and the install point. Not the built host | `App`, `App<TPack>`, `IApp`, `NotesApp`, `EngineApp`; the concept record `App` |
| Host | The code that starts one kind of app. The host class makes, runs and stops the built host. The VSA layer `Host` holds this code and the app class | `AppHost`, `VsaHost`, `WebVsaHost`, `ConsoleVsaHost`; the layer record `Host` |
| Built host | The Microsoft host object that a host class builds | `IHost` (Microsoft.Extensions.Hosting) |
| Registration | One entry in the `IServiceCollection` of an app | `AddScoped`, `AutoRegistration`, `AddPipeline`, the `Add<Name>(services, types)` extensions |
| Service implementation | A pair of a service type and its implementation type that `AppTypes` finds. It is not registered yet | `ServiceImplementation`, `AppTypes.Implementations` |

## Block kinds

| Kind | Meaning | Example |
|---|---|---|
| App | A block that runs. Its app class is the check entry and the install point | `NotesApp`, `EngineApp` |
| Pack | A block with one architecture and its kernel | `packs/Vsa/` |
| Framework kernel | Free base code that each app has, with no layer. It owns the host | `framework/Kernel/`, `AppHost` |
| Pack kernel | The layer bases of a pack. It extends the framework kernel | `packs/Vsa/Kernel/`, `VsaHost` |
| Component | Free code that a kernel installs by name | `framework/Components/Messages/` |
| Module | A component that the host finds in the app. It ships with the app | A feature module, a plugin module |
| Library | A block with no install point | `engine/Versions/` |

Each block kind has a concept record in `Umpire.Architecture.Blocks`: `App`, `Pack`, `Kernel`, `Component`, `Module` and `Library`, on the base `BlockKind`. A component can have a layer shape. That does not make it a module. A plugin is a VSA module kind: the base of the same layers in each module. See `blocks.md`.

## Definition terms

| Term | Meaning | Code names |
|---|---|---|
| Block | A folder tree of code with one entry. Also the top part of a definition | `Block` (the top part), `Block<TArchitecture>`, `BlockKind` (the base of the block kind records) |
| Independent block | A block with its own definition and check entry, for example an app or a pack | |
| Owned block | A block that gets its shape from the definition around it, for example a feature module | |
| Install point | The class or method that tells the host what to install from a block | `Pack.CreateHost()` |
| Block folder | The folder where a definition applies: the folder of the nearest check entry, or `--block` | `UmpireBlock` |
| Check entry | The pointer from a block to its architecture. Each block has one | An app class, a `Block<TArchitecture>` subclass, the `AssemblyMetadata` `Umpire.Architecture`, an `umpire.json` file |
| Block place | A folder that an outer definition marks as the place of another block. The outer definition sees that block as one part | `AddBlock<T>()`, `AddBlockGroup<T>()` |
| Concept | A kind of part that a definition names | `Concept`, `External`, `BlockKind`, `Layer` |
| Part | One place in the folder tree of a definition | `Part`, `PartGroup` |
| Part set | A named list of parts that a definition uses more than one time | The part set helpers of `VsaArchitecture`: `AddContractParts`, `AndHandlerParts`, `ForContractParts`, `UsesContractParts`, `ForHandlerParts` |
| Layer | A VSA concept for one kind of code, with its base in the pack kernel | `Layer`: `Domain`, `Contracts`, `Handlers`, `Presenters`, `Views`, `Infrastructure`, `Host`, `Composition`, `Slice` |
| Module | A module of the app: a feature module or a plugin module. The record `Module` is the concept. The part `Add<Module>()` holds the module class | The record `Umpire.Architecture.Blocks.Module`, the classes `Umpire.Kernel.Module` and `Plugin` |
| Module kind | A group of modules with one shape. Each module kind record inherits `Module` | `Feature`, `Plugin`, the `EcsFeature` of a pack variant |
| External part | A part that is a DLL, not a folder | `External`, `Kernel`, `IsExternalPartType` |
| System assembly | A .NET assembly that the engine does not load from the app: `System.*`, `Microsoft.*`, `netstandard`, `mscorlib` | `IsSystemAssembly` |
| Define method | A `virtual` method of `VsaArchitecture.Define` for one base group | `DefineFeature(PartGroup feature)` |
| Rule | Who may use whom | `rules.For<A>().Uses<B>()` |
| Queries | The answers of the rules on plain facts (`architecture/Queries/`) | `PartOf` (which part), `DecisionFor` (allow or deny), `Counts`, `Matches` |
| Count | A rule statement that counts the types of a concept in each instance of a group, or the types that take each type of a part | `Has<T>()` |
| Match | A rule statement that a type takes a type argument of a concept, in the same instance of a group | `Takes<T>().InSame<G>()` |

## Messages

| Term | Meaning | Code names |
|---|---|---|
| Kind | Always "kind of X": one of a fixed set of shapes of X. Write the noun: block kind, module kind, message kind | `Add<Kind>Handlers`, the block kind records, `Feature`, `Plugin` |
| Message kind | Request, command or event | `IRequest`, `ICommand<T>`, `IntegrationEvent` |
| Send | Give a command or a request to its one handler | `ICommandSender.SendAsync`, `IRequestSender.SendAsync` |
| Publish | Give an event to each of its handlers | `IIntegrationEventPublisher.PublishAsync` |
| Pipeline step | A Before or After stage of one pipeline, for each message of that pipeline | `BeforeStep`, `AfterStep`, `BeforeStep<T>`, `AfterStep<T>` |
| Hook | A Before or After hook of one message kind. It is in its own slice | `BeforeCommandHook<T>`, `AfterEventHook<T>`; the parts `Handlers.Commands.Hooks`, `Handlers.Events.Hooks` |

## Engine terms

| Term | Meaning | Code names |
|---|---|---|
| Engine step | One feature module of the engine with one command | `EngineStep<TResult>` |
| Finding | One result line: code, severity, message, file and line | `Finding` |
| Baseline | The errors that `umpire-baseline.txt` accepts | |
| Rule ID | `UMP` and six digits: area (2), group (2), case (2). Area `01` analyzers, `02` rule findings, `03` engine run | `UMP030201` |

## Tool terms

| Term | Meaning | Code names |
|---|---|---|
| Template group | The template files of one `umpire` command | `TemplateGroup` |
| Setup task | One change that `umpire init` and `umpire new app` make to an app | `SetupTask`, the list `SetupTasks` |

## Terms with no own entry

| Word | Use instead |
|---|---|
| Step, alone | "Engine step" or "pipeline step" |
| Framework, for a .NET assembly | "System assembly" |
| Framework, for a DLL part of a definition | "External part" |
| App, for the `IHost` | "Built host" |
| Registration, for a found type pair | "Service implementation" |
