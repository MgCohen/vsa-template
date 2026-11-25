# Vocabulary

Each term has one meaning in the code, the docs and the messages. Umpire is the product, and VSA is one architecture pack of Umpire.

To make a first app and fix a first rule error, see [getting-started.md](getting-started.md).

A code name with the mark "(D6)" comes with the sender rename of the D6 work package. The old name is in the same row.

## Product and packs

| Term | Meaning | Code names |
|---|---|---|
| Umpire | The product: framework, rule engine, tool and SDK | `Umpire.Kernels`, `umpire` tool |
| Framework | The Umpire code in `framework/`: the core and the components. Not a .NET assembly, and not an external part | `framework/Kernel/`, `framework/Components/` |
| Pack | One architecture with its definition, bases and rules | `packs/Vsa/`, `VsaArchitecture`, `VsaKernel` |
| Pack variant | A pack whose architecture inherits the architecture of another pack and adds parts and rules | `App/Packs/Ecs/`, `EcsArchitecture : VsaArchitecture` |
| VSA | The Vertical Slice Architecture pack | `Umpire.Vsa.*`, `VsaArchitecture` |
| Architecture | A definition: concepts, parts and rules | `VsaArchitecture : Architecture` |

## Apps and hosts

| Term | Meaning | Code names |
|---|---|---|
| App | A block that runs. Its app class is the check entry and the lifecycle: `Start`, `Run`, `Stop` | `App`, `App<TKernel, THost>`, `IApp`, `NotesApp`, `EngineApp`; the root record `App` |
| Host | The platform of an app. The host class builds the services and disposes them. It does not run or wait | `AppHost`, `WebHost`, `ConsoleHost`; the layer record `Host` |
| Kernel | The install point of an architecture. `Core` installs the core services, then the kernel of the pack installs the layers | `Kernel`, `Core`, `VsaKernel` |
| Operation | A request that a host exposes, with its group, name and effect | `Operation`, `Effect` |
| Stop request | A call from app code that stops the app with an exit code | `AppStop.Request` |
| Registration | One entry in the `IServiceCollection` of an app | `AddScoped`, `AutoRegistration`, `AddPipeline`, the `Add<Name>(services, types)` extensions |
| Service implementation | A pair of a service type and its implementation type that `AppTypes` finds. It is not registered yet | `ServiceImplementation`, `AppTypes.Implementations` |

## Block kinds

| Kind | Meaning | Example |
|---|---|---|
| App | The root of the app code. It runs, and its app class is the check entry | `App/`, the root record `App`, `NotesApp`, `EngineApp` |
| Definition | The root of the architecture class of a block, with its records and externals | `Definition/`, the root record `Definition` |
| Pack | A block with one architecture and its kernel | `packs/Vsa/`, the record `Pack` |
| Core | Free base code that each app has, with no layer. It owns the hosts | `framework/Kernel/`, the root record `Core`, `Architecture<TKernel>` |
| Kernel root | The layer bases of a pack: the `kernel` part of `Define` | `packs/Vsa/Kernel/`, the record `Kernel` |
| Component | Code that travels together, with no entry point. A kernel can install it by name | `framework/Components/Messages/` (`MessagesComponent`), `engine/Versions/` |
| Module | A component that the host installs. It ships with the app | A feature module, a plugin module |
| Slice | An owned block of a feature. It has no entry, and it does not travel | `App/Features/<Feature>/Slices/<Slice>` |

Each block kind has a record on the base `Block`. `Umpire.Architecture.Blocks` has `App`, `Definition`, `Kernel` and `Component`. `Umpire.Kernels.Blocks` has `Core`, `Module` and `Pack`. `Feature`, `Plugin` and `Slice` are VSA records. A component can have a layer shape. That does not make it a module. A plugin is a VSA module kind: the base of the same layers in each module. See `blocks.md`.

## Definition terms

| Term | Meaning | Code names |
|---|---|---|
| Block | A folder tree of code with one entry. A block record is a concept with a body | `Block` (the base of the block records), `Block<TArchitecture>` (the check entry) |
| Root | The top part of a definition. Its children are the roots of `config.Roots` and the kernel root. `Define(Part root, Part kernel)` gets the `App` root and the kernel root | `ResolvedArchitecture.Root`, the name `(root)`, `Roots.Add<T>()` |
| Independent block | A block with its own definition and check entry, for example an app or a pack | |
| Owned block | A block that gets its shape from the definition around it, for example a feature module | |
| Install point | The class or method that tells the host what to install from a block | `App.CreateHost()` |
| Block folder | The folder where a definition applies: the folder of the nearest check entry, or `--block` | `UmpireBlock` |
| Check entry | The pointer from a block to its architecture. Each block has one | An app class, a `Block<TArchitecture>` subclass, the `AssemblyMetadata` `Umpire.Architecture`, the key `umpire.architecture` in a `.umpireconfig` file |
| Block place | A folder that an outer definition marks as the place of another block. The place gets the body of the record | `AddBlock<T>()`, `AddBlockGroup<T>()` |
| Overlay | A part that a definition adds to each placed part of a concept | `For<T>().Add<TPart>()`, `For<T>().ExtendedBy<TBlock>()` |
| Owner | The architecture that a concept belongs to. Only the owner or a derived architecture can place it | `OwnedBy`, `IOwnedBy<T>`, `Concept<TOwner>` |
| Concept | A kind of part that a definition names. It has data only | `Concept`, `External`, `Block`, `Layer`, `OfTypes` |
| Part | One place in the folder tree of a definition | `Part`, `PartGroup` |
| Part set | A named list of parts that a definition uses more than one time | The part set helpers of `VsaArchitecture`: `ForContractParts`, `UsesContractParts`, `ForHandlerParts` |
| Layer | A VSA concept for one kind of code, with its base in the kernel root of the pack | `Layer`: `Domain`, `Contracts`, `Handlers`, `Presenters`, `Views`, `Infrastructure`, `Composition`. The core concepts `Host` and `Hosts` |
| Module | A module of the app: a feature module or a plugin module. The record `Module` is the concept. The module class is at the top of the module folder, and it binds to the module part | The record `Umpire.Kernels.Blocks.Module`, the classes `Umpire.Kernels.Module` and `Plugin` |
| Module kind | A group of modules with one shape. Each module kind record inherits `Module` | `Feature`, `Plugin`, the `EcsFeature` of a pack variant |
| External part | A part that is an opaque DLL, not a folder | `External`, `IsExternalPartType` |
| Module class | The one class at the top of a module folder that inherits `Umpire.Kernels.Module`. It binds to the module part | `HasTypes<Umpire.Kernels.Module>().Exactly(1)` |
| System assembly | A .NET assembly that the engine does not load from the app: `System.*`, `Microsoft.*`, `netstandard`, `mscorlib` | `IsSystemAssembly` |
| Rule | Who may use whom | `rules.For<A>().Uses<B>()` |
| Architecture rule | One class of the engine that gives the findings of one code. It sets its number; the group base sets the group | `DeniedUseRule : UseRule` |
| Queries | The answers of the rules on plain facts (`architecture/Queries/`) | `PartOf` (which part), `DecisionFor` (allow or deny), `Counts`, `Matches` |
| Count | A rule statement that counts the types of a concept in each instance of a group, the types that take each type of a part, or the types that inherit a runtime type | `Has<T>()`, `HasTypes<T>()` |
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
| Rule ID | `UMP` and six digits: area (2), group (2), case (2). Area `01` analyzers, `02` rule findings, `03` engine run | `UMP030401` |

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
| Registration, for a found type pair | "Service implementation" |
