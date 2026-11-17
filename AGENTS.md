# AGENTS.md

Instructions for AI coding agents (and humans) working in this repository. This file is model-agnostic: do not add tool-specific files such as `CLAUDE.md`; put shared guidance here.

## Project

Umpire: a framework and a rule engine that check the architecture of an app after each build. Its first architecture pack is VSA (Vertical Slice Architecture). The repository has the base infrastructure, the scaffolding and the architecture rules, for use as the start of new projects. The reference apps are in the consumer repos (`docs/consumers.md`). Umpire is a working name.

## Layout

| Path | Purpose |
|------|---------|
| `architecture/` | Umpire.Architecture: the definition API (`Architecture`, `Concept`, `Part`, `PartGroup`, `RuleBook`, `Options`), the block kind records in `Concepts/Blocks/` (namespace `Umpire.Architecture.Blocks`: `App`, `Pack`, `Kernel`, `Component`, `Module`, `Library`) and the check entry base `Block<TArchitecture>` and `Queries/` (the answers of the rules on plain data: `ResolvedArchitecture`, the one compiled form of a definition, with `PartOf`, `DecisionFor`, the counts and the matches). No ArchUnitNET. `Scan/`: `LoadedTypes`, the one load-safe type read. `Testing/`: `ArchitectureTest<TArchitecture>`, the base of the tests of a definition. `Markers/` (Umpire.Markers): `[AutoRegister]`, `[TypeArgument]` and the IL2CPP markers `[Preserve]`, `[RequireDerived]`, `[RequireImplementors]`. It references no other C# project. The one exception is the build: the root `Directory.Build.props` gives it `tools/sdk/Polyfills/IsExternalInit.cs` and the style switches of the repository (the rules are in the root `.editorconfig`), and `Directory.Build.targets` gives it the kernel analyzer and its rule values, as for each framework project |
| `framework/Kernel/` | Umpire.Kernel: what each app needs to start, with no layer: `App/` (`App`, `App<TPack>`, `IApp`, `Pack`, `PackArchitecture`), `Hosting/` (`AppHost`), `Modules/` (`Module`), `Scan/` (`AppTypes`), `Factories/`, `Services/`. `Analyzers/` (Umpire.Kernel.Analyzers): the analyzer base `RuleAnalyzer` and the rules about any code |
| `framework/Components/` | Optional blocks that an architecture takes. `Messages/`: `Contracts/` (Umpire.Messages.Contracts: `IMessage`, `Envelope`, `Result`) and `Handlers/` (Umpire.Messages.Handlers: the pipeline) |
| `packs/Vsa/` | The VSA pack: `VsaPack.cs` (`Vsa<THost>`), `Architecture/` (Umpire.Vsa: `VsaArchitecture`, `Layer`, the VSA layers and the module kinds `Feature` and `Plugin` in `Concepts/`), `Kernel/` (the layer bases: `Contracts/`, `Handlers/`, `Domain/`, `Infrastructure/`, `Presenters/`, `Views/`, `Composition/` with `VsaHost`, `ConsoleVsaHost` and `Plugin`, `Hosts/` with `WebVsaHost`) and `Tests/` (`Analyzers/`: Umpire.Vsa.Analyzers, the VSA rules; `Unit/`: Umpire.Vsa.Testing.Unit, with `VsaUnitTest` and `VsaArchitectureTest`) |
| `engine/` | The rule engine app (a VSA console app: Blocks, Versions, LoadApp, Rules, Baseline, Report, and the plugins Steps and BuiltApp). `Versions/` (Umpire.Engine.Versions): the version check `UMP030201`, also in the `umpire` tool. Its root project `engine/Engine.csproj` makes the build hook package Umpire.Engine.Build, with `Umpire.Engine.Build.targets`. See "The rule engine" below |
| `tools/cli/` | The `umpire` tool (package Umpire.Cli) and its templates (`Templates/`) |
| `tools/sdk/` | Umpire.Sdk and Umpire.Sdk.Web (`Web/`): the build setup of each project: the general framework references, the default pack (`Umpire.Vsa`, `UmpireDefaultPack`), the source folders and the compiler polyfills (`Polyfills/`). The values of the Umpire rules come with each analyzer package |
| `tests/` | Tests of the framework itself, with fixtures. Does not ship |
| `docs/` | Architecture decisions, conventions, and guides. The first app is in `docs/getting-started.md`, the terms are in `docs/vocabulary.md`, and the kinds of block in `docs/blocks.md` |

The framework groups and their references are in `docs/framework-layout.md`.

There are three kinds of module: the VSA pack (`packs/Vsa/`), a plugin module and a feature module (the code of the app). A feature module is a folder `Features/<Feature>/`, and a plugin module is a folder `Plugins/<Plugin>/`. The code is in `Domain/`, `Domain/Shared/` (the published domain of the module), `Infrastructure/` and `Presenters/`. A feature module has its other code in `Slices/<Slice>/`. A plugin module has no slices: it has the layout of the framework, with one folder for each layer, for example `Handlers/Commands/`. Its root class inherits `Plugin` (`UMP020101`, `UMP020301`). The word "Shared" is only `Domain/Shared/` and `Slices/Shared/`. See `docs/slice-layout.md`. An app can also have its own packs in `Packs/<Name>/`: each one is an inner block with its pack class as its check entry, and a pack variant there adds module kinds and rules to `VsaArchitecture`. See `docs/pack-variants.md`.

An app has one root project in the block folder, for example `tests/Fixtures/Notes/Notes.csproj`. It compiles each file in its folder tree. All other projects are optional, for example a test project or a separate assembly. A parent project does not compile the folder of a nested project, and no project compiles `bin/`, `obj/` or the folders in `UmpireExcludedFolders` (for example `Assets/`). The folders select the layers, and the rule engine checks each use. The name of a module is the last part of the namespace of its root class, and one assembly can have many modules. Each namespace starts with the app name and follows the folders, for example `Notes.Features.Notebook.Slices.CreateNote` or `Notes.App.Host`. Put the `using` directives inside the namespace, so a type of Umpire wins over a namespace of the app with the same name, for example `App`. This is a guideline, not an enforced rule. If the file is inside the namespace of the concept records (a pack variant), put some `using` directives outside the namespace. If that is not enough, add an alias inside the namespace. See "`using` placement" in `docs/architecture-engine.md`.

The block folder is the folder of the nearest check entry: the app class, a `Block<TArchitecture>` subclass (for example a pack class), the `AssemblyMetadata` `Umpire.Architecture`, or an `umpire.json` file. For an app, it is the folder of the root project, also when `global.json` is in a folder above it. An outer definition marks the place of an inner block with `AddBlock<T>()` or `AddBlockGroup<T>()` (see "Blocks and check entries" in `docs/architecture-engine.md`). The kinds of block (app, pack, kernel, component, module, library), their entries and the composition guidelines are in `docs/blocks.md`. In a Unity project (`ProjectSettings/ProjectVersion.txt` in the block folder), the module groups are in `Assets/<AppName>/` and in a sibling folder of `Assets/` (default `Source/`), the views are in `Assets/<AppName>/Views/`, and the architecture checks also read `Library/ScriptAssemblies/` (see `docs/architecture-engine.md`). The Unity build parts are in the `vsa-unity` package (MgCohen/vsa-unity#8). In this repository, `engine/`, `tests/Fixtures/` and the templates set the block folder (`UmpireBlock`) for each app, because the repository has one `global.json`. A plugin module is the base of the same layers in each module, as the framework is. See `docs/architecture-rules.md`.

Umpire.Sdk gives each project the general packages (Umpire.Kernel, Umpire.Markers) and the default pack `Umpire.Vsa`. The analyzer package of the pack holds the kernel analyzer DLL and the kernel rule values too (pinned choice, `framework/Kernel/Analyzers/Umpire.KernelBundle.targets`), so in package mode the SDK does not add the Umpire.Kernel.Analyzers package. The code selects the pack (the app class, for example `App<Vsa<WebVsaHost>>`). A package only makes a pack available: the pack package depends on the base of each layer and on its analyzer package, and the analyzers of a pack run only in a compilation that selects that pack (`RuleAnalyzer.PackAssembly`, `PackSelection`). Another pack is a normal `PackageReference`, and `UmpireDefaultPack=false` leaves out `Umpire.Vsa`. A pack variant in `Packs/<Name>/` derives from `Vsa<...>`, so it selects VSA. Add an Umpire library that is not a layer base (for example `Umpire.Vsa.Hosts.Web`) as a normal reference, with the version of Umpire.Sdk. The rule engine checks each Umpire version (`UMP030201`). Inside this repository, the engine, the fixtures and the templates import Umpire.Sdk from source (`tools/sdk/Umpire.Sdk.Dev.props`). A consumer project is `<Project Sdk="Umpire.Sdk/<version>" />`, or `<Project Sdk="Umpire.Sdk.Web/<version>" />` for a web root project. The Umpire SDK sets the output to `artifacts~` in the block root (the folder of the nearest `.umpireconfig`), gives `.umpireconfig` to the compiler, adds `Umpire.Engine.Build` to the root project and sets up NUnit in each `*.Tests` project (see "Packages of an app" in `docs/consumers.md`).

All build output goes to `artifacts~/`, so no `bin/` or `obj/` folder is next to the code. Each block folder (`engine/`, each app in `tests/Fixtures/`, `tools/cli/Templates/`) has its own `artifacts~/`. The library projects and the test projects share `artifacts~/` in the repository root. Git ignores `Umpire.slnx`. To make it, run `dotnet run solution.cs`. The script adds each `.csproj` in the repository, except `tests/Fixtures/`. It keeps `tests/Fixtures/Notes/` and `tests/Fixtures/PackVariant/Packs/Ecs/Tests/`, because they are tests of a consumer. After you add, move or remove a project, run the script again. Do not edit `Umpire.slnx` by hand.

The app has one entry class, `MyApp : App<TPack>` (Umpire.Kernel, portable), in `App/Host/<Name>App.cs`. The pack joins the architecture and the host: Notes is `App<Vsa<WebVsaHost>>`, and the engine is `App<Vsa<EngineArchitecture, ConsoleVsaHost>>`. `Vsa<THost>` (Umpire.Vsa) uses `VsaArchitecture`. The app class file imports `Umpire.Kernel`, `Umpire.Vsa` and the namespace of the host. The definition of an app with its own rules imports `Umpire.Vsa.Definition` (`VsaArchitecture`, the layer records, `Feature` and `Plugin`) and `Umpire.Architecture.Blocks` (the block kind records, for example `App` and `Pack`). `Program.cs` is only `return new MyApp().Run(args);`. The app class can override `ConfigureServices` (last), `OnStart`, `OnStop` and `Modules()`. Each `Run` makes a new host from the pack. The host decides if `Run` waits or returns. `Stop()` stops the app instance, and `Run` can then start it again. The rule engine reads `Pack.Architecture` of the app class (`UMP030302` if the app has no app class). To add app rules, derive an architecture from `VsaArchitecture`, override `Rules(RuleBook)`, and use `Vsa<TArchitecture, THost>`. The host finds the modules and the views by itself. To add a feature module, add its files under `Features/`. Do not edit `Program.cs`. Each module root has exactly one class that inherits `Module` or `Module<TSettings>` (`Plugin` or `Plugin<TSettings>` in a plugin module), else startup fails. `Configure(IServiceCollection)` is virtual: a module root with no registrations is one line, for example `public sealed class PingModule : Module;`.

The host (`AppHost`, Umpire.Kernel) is the composition root. `AppHost.Compose(services, app)` has a fixed order: the core (`IConfiguration`, logging, `AppTypes`, auto-register), then `Install(services, types)` of the host, then the internal `Module.Compose(services, configuration)` of each module in `Order` (it calls `Configure(services)`, or for `Module<TSettings>` it binds the settings and calls `Configure(services, settings)`), then `app.ConfigureServices`. `VsaHost` installs each VSA layer by a direct call, and `WebVsaHost` (Umpire.Vsa.Hosts.Web) also adds the HTTP API of the requests and the views. There is no installer scan. A Unity app with its own container calls `new Vsa<UnityVsaHost>().CreateHost().Compose(services, app)` and builds the container itself. A module replaces a default with `services.Replace<TService, TImplementation>()`. A factory (`IFactory<T>`, `services.AddFactory`) makes objects that the container cannot make, for example Unity objects. `AppTypes` finds the modules and the views root (`ViewsRoot`) in the loaded assemblies and the `.dll` files of the app. `Compose` makes one `AppTypes` instance (`new AppTypes(app.Modules())`), and it reads each assembly one time. Other code gets its types from that instance, for example with `ConcreteTypesOf(typeof(TBase))`. The reflection uses are in `docs/composition.md`. Module code registers services on `IServiceCollection` and resolves them with `IServiceProvider`. These interfaces are container-neutral, so do not add an Umpire container interface. To add a kind of registration, write a public `Add<Name>(this IServiceCollection services, AppTypes types)` extension in the project of its layer, and call it in `VsaHost.Install` (or in `WebVsaHost.Install` if it needs ASP.NET Core). It finds its types with `types.Implementations(typeof(TBase))`. See `docs/composition.md`.

Each message goes through a pipeline to its handler (Umpire.Messages.Handlers). A handler takes `Envelope<TMessage>` and returns `Result<T>`, and each send (`ICommandSender`, `IBus`, `IIntegrationEventPublisher`) returns `Result<T>`. `Result`, `Error` and `Envelope` are in Umpire.Messages.Contracts, next to `IMessage`: domain code does not use them. A run is: validators, before steps, Before hooks, the handler, After hooks, after steps. Validators (`Validator<T>`) are found by type, and each is only in the slice of its handler. A hook (`BeforeCommandHook<T>`, `AfterCommandHook<T, TResult>`, `BeforeEventHook<T>`, `AfterEventHook<T>`) is in its own slice, and its name ends with `Hook` (`UMP010209`). No hook runs for a request. A step (`BeforeStep`, `AfterStep`, or the typed `BeforeStep<T>`, `AfterStep<T>`) belongs to one pipeline. A module defines a custom pipeline with `services.AddPipeline(new PipelineBuilder<TMessage>()...)`: handlers, hook bases (`HooksOf`), runner, steps, all by type. The handler bases select the pipeline of a message, and each kind adds its default pipeline the same way. Each message runs at once (`InlineRunner`), unless the pipeline names another runner. See `docs/handler-pipeline.md`.

A presenter shows the events of its own module: it implements `IPresenterEvent<TEvent>`, and it gets each event, also when no view shows it. A view only renders: it inherits `PresenterWatcher` (one or more presenters) or `PresenterComponent<T>` (one presenter). A presenter does not publish events (`UMP010204`). See `docs/presenter-events.md`.

## The rule engine

The rule engine checks the architecture of an app after each build. It is a VSA console app in `engine/`: its host `ConsoleVsaHost` sends its one entry command, `RunEngineCommand`. Its handler is the composition root, one line for each step: Blocks (finds the block of the project and its check entry, `UMP0301xx`), Versions (checks that the app and the engine have one Umpire version, `UMP030201`), LoadApp (creates the definition), Rules (checks the layer rules), Baseline (removes the errors that `umpire-baseline.txt` in the block folder accepts), then Report (sorts the findings and writes `file(line,col): error CODE: text` lines). The rules come from `architecture/Queries/` on plain data. Only the plugin `engine/Plugins/BuiltApp/Infrastructure/` reads the DLL and PDB files: `BinReader` reads the bin one time in the Blocks step (`MetadataLoadContext`, result `BinIndex`), `AppLoader` creates the one definition type, and ArchUnitNET reads the type facts for the rules. The engine definition (`engine/App/Host/EngineArchitecture.cs`) gives ArchUnitNET only to `Infrastructure`. A step is a feature module with one command that inherits `EngineStep<TResult>` (plugin `engine/Plugins/Steps`). The Steps plugin also has the pipeline parts in `Infrastructure/`: the runner, the after step that records the findings, and the step log. A pipeline step is in `Infrastructure/` of a module or of a plugin, and a `Validator<T>` is only in the slice of its handler. To add or remove a step, add or remove its line. The Steps pipeline collects the findings of each step. If a step throws, it records `UMP030001` and the run continues. If an earlier step fails, `Run` skips the step with an `if`, and `StepLog.Skip` records `UMP030002`. `--failfast true` stops the run at the first throw.

To add a step:

1. Add a feature module `engine/Features/<Step>/` with one slice.
2. Write the command: `public sealed record <Name>Command(...) : EngineStep<<Name>Result>, IStepGroup { public static int Group => <n>; }`. The group is the place of the step in `Run` (Blocks 1, Versions 2, LoadApp 3, Rules 4, Baseline 5, Report 6). Its inputs are the results of earlier steps, for example `AppInput App`. If the step needs an earlier step that can fail, write `earlier.IsSuccess ? await commands.SendAsync(...) : log.Skip<<Name>Command, <Name>Result>()` in `Run` (`UMP030002`).
3. Write the result: `public sealed record <Name>Result(IReadOnlyList<Finding> Findings, ...) : IStepResult`. The Steps pipeline adds its findings to the report.
4. Write the handler: `CommandHandler<<Name>Command, <Name>Result>`. For an expected failure, return an `Error` with a code of the step: put each code in one `<Name>Codes` class, as `StepCode.Of<<Name>Command>(n)` (`UMP03` + group + case). Do not catch exceptions: the pipeline records them as `UMP030001`.
5. Add one line to `Run` in `RunEngineHandler`: `var x = await commands.SendAsync(new <Name>Command(...), ct);`.
6. Add a case to `tests/Engine.Tests`.

An app installs it with one package on its root project: `<PackageReference Include="Umpire.Engine.Build" PrivateAssets="all" />`. Switches: `-p:UmpireCheck=false`, `-p:UmpireCheckMode=Warn`, `-p:UmpireUpdateBaseline=true` (writes the errors of the build to `umpire-baseline.txt`, see `docs/architecture-engine.md`). A test project does not check the architecture: only the build of the host does. Run it by hand with `dotnet engine/artifacts~/bin/Engine/debug/umpire-engine.dll --project <host project folder> --block <block folder> --bin <host output>`.

## Tests

Run all tests with `dotnet test --solution Umpire.slnx`. Run one project with `dotnet test --project tests/Framework.Tests`. The tests use NUnit on Microsoft.Testing.Platform (set in `global.json`). NUnit also runs in Unity (Unity Test Framework), and xUnit does not.

| Path                     | Put here                                                                  |
|--------------------------|---------------------------------------------------------------------------|
| `tests/Framework.Tests/` | Tests of one framework layer. One folder for each area, for example `Settings/`, `Host/` |
| `tests/Consumer.Tests/`  | Tests that pack the framework and build a copy of `tests/Fixtures/Notes` as a consumer |
| `tests/Cli/`             | Tests of the `umpire` tool and its templates                                 |
| `tests/Architecture.Tests/` | Tests of the rule engine core, on the fixture apps in `tests/Fixtures/Architecture/` |
| `tests/Composition.Tests/` | Tests of `Compose`, the layer registrations and the console host, with no ASP.NET Core |
| `tests/Engine.Tests/`    | Tests of the rule engine app, in the test process, on `tests/Fixtures/Notes` |
| `tests/Rules.Tests/`     | Tests of the analyzers (Roslyn analyzer test library) and of the formatting rules in a real build |
| `tests/Fixtures/`        | Small modules and apps that tests use. They are not in `Umpire.slnx`, except Notes |
| `tests/Shared/`          | Helpers: `Dotnet` (run the CLI), `LocalFeed` (pack), `Workspace` (consumer copy) |

Each first folder in `tests/Fixtures/` is one block:

| Block | Contains |
|---|---|
| `Architecture/` | The fixture apps of `tests/Architecture.Tests` |
| `Blocks/` | The check entry cases of Engine.Tests: an app with an `AssemblyMetadata` block (`Lib/`), two pack blocks (`Packs/`) and an `umpire.json` block (`Tools/`) |
| `Built/` | The built fixtures |
| `Modules/` | The referenced modules of the framework, composition and rule tests |
| `Notes/` | A full web app with its own unit tests. Engine.Tests and Consumer.Tests use it |
| `TwoApps/`, `NoConstructorApp/` | Engine cases: a block with two apps, an app class with no public constructor |
| `PackVariant/` | An app with its own pack variant `Packs/Ecs/` and the module kind `EcsFeature`. Engine.Tests and Architecture.Tests use it |

There are two kinds of fixture:

- **Referenced fixture:** a module that must build. A test project references it with `ProjectReference`, and it builds in source mode (`tests/Fixtures/Directory.Build.props`).
- **Built fixture:** a test builds it out of process to check the build result. Write its projects in the consumer form (`<Project Sdk="Umpire.Sdk" />`). Use `Workspace.Create(folder)`, then `workspace.Dotnet("build")`.

A new test project goes in `tests/<Name>.Tests/`, then run `dotnet run solution.cs` to add it to `Umpire.slnx`. Its project file has only its own references. `tests/Directory.Build.props` gives each project whose name ends with `.Tests` the NUnit settings: `OutputType` Exe, `EnableNUnitRunner`, the NUnit packages and `using NUnit.Framework`.

A consumer tests its own definition with `ArchitectureTest<TArchitecture>` (`VsaArchitectureTest` for a VSA app): "this type in this folder binds to this part" and "this use is allowed". See "Tests of a definition" in `docs/architecture-engine.md`.

`LocalFeed` packs all framework packages one time for each test process, with version `0.0.0-test`. A `Workspace` restores from that feed with its own NuGet cache, in a short temporary folder.

The public surface of `tests/Shared` (namespace `Tests.Shared`). Other test projects use it, so keep these names stable:

| Member | Use |
|--------|-----|
| `Repo.Root`, `Repo.Path(params string[])` | The repository root, and a path in it |
| `LocalFeed.Path`, `LocalFeed.Version`, `LocalFeed.Packages` | The feed folder (packs on first use), `0.0.0-test`, the package ids. It packs each packable project under `framework/` |
| `Workspace.Create(string copyFrom)` | Copies a folder to `Src` and writes `global.json` in `Src` (the block folder), and `nuget.config`, `Directory.Build.props` and `Directory.Packages.props` in `Root` |
| `workspace.Root`, `.Src`, `.Env`, `.Dotnet(params string[])`, `.Start(params string[])`, `.Dispose()` | Run dotnet in `Src` with the workspace NuGet cache. `Dispose` deletes the folder |
| `Dotnet.Run(workDir, env, params string[])`, `Dotnet.Start(...)` | Run the dotnet CLI without the MSBuild variables of the test host |
| `DotnetResult(Command, ExitCode, Output)`, `.Succeeded`, `.EnsureSuccess()` | The result of a run |
| `TempFolder.Create()`, `.Stable(name)`, `.Copy(from, to)`, `.Delete(path)` | Short temporary folders. `Stable` gives the same empty folder for the same name in each run |

## Core principles

- **Organize by slice, not by layer.** Each slice is one folder, `Slices/<Slice>/`, with its request, response, events, handler and helpers. A slice has one kind of handler: a request handler, an event handler or a command handler.
- **Slices are independent.** A slice uses only its own types and `Slices/Shared/`. An event is in the slice that publishes it.
- **Modules talk with commands and events.** A command goes to one handler of another module. Name the types `<Name>Command`, `<Name>Result` and `<Name>Handler`. See `docs/slice-layout.md`.
- **Keep shared code small.** Promote code to a shared location only when two or more slices genuinely need it.
- **Rules are enforced by tests.** Architecture rules live in `tests/` and must pass.

## Add a check

The levels, rules and base classes are in `docs/architecture-rules.md`. Use a library first: the built-in .NET analyzers, Roslyn, ArchUnitNET or NUnit. Tests use NUnit, so they also run in Unity.

1. If a built-in .NET rule does the check, set its severity in the root `.editorconfig`. This is the style of this repository only: Umpire.Sdk ships no native style, and a consumer sets its own.
2. If one file shows the problem, write a rule class on the group base of its DLL: `VsaRule` (group `02`) in `packs/Vsa/Tests/Analyzers/` for a VSA rule, `KernelRule` (group `01`) in `framework/Kernel/Analyzers/` for a rule about any code. The analyzer of that DLL (`VsaAnalyzer` or `KernelAnalyzer`, both on `RuleAnalyzer`) finds and runs it. The rule sets only its `Number` (the next free number of its group), its `Title` and its `Message`. The base makes the ID: `UMP01` + group + number. Each value that a rule uses (a limit, a list, an order) is an option: list it in `Options` with its default, for example `UmpireOption.Int(MaxKey, 120)`, and read it with `Values(...)`. A consumer changes a value in `.umpireconfig`. An option with no default and no value gives `UMP010001`, and so does a value that is not valid. A rule with a maximum inherits `LimitRule` and sets `DefaultMax`. A rule about any code, not only modules, sets `IsModuleRule` to false: then it also checks the framework code.
3. Else change the rules of the VSA definition (`packs/Vsa/Architecture/VsaArchitecture.cs`). Add a use rule (`Uses`, `Denies`), a count (`Has<T>()`) or a match (`Takes<T>()`). The rule engine (`UMP020101`, `UMP020102`, `UMP020201`, `UMP020202`, `UMP020301`, `UMP020302`) checks them. See `docs/architecture-engine.md`.
4. Write a message that gives what is wrong, the type names, and the fix.
5. Add tests in `tests/Rules.Tests/`. For an analyzer, use the Roslyn analyzer test library (`AnalyzerTests.cs`). For the architecture engine, add a case to `tests/Architecture.Tests/` with the fixture apps in `tests/Fixtures/Architecture/`.
6. Add the rule to the table in `docs/architecture-rules.md`.

## Working agreements

- Follow the conventions of the surrounding code (naming, structure).
- Do not write comments in C# code (`UMP010106`). Put the information in a name: a type, a member, a variable or a test.
- Both namespace forms are allowed (block and file-scoped). A MonoBehaviour must use a block namespace in Unity.
- Add or update tests with every behavior change.
- Document notable architecture decisions in `docs/`.
- Keep this file current when conventions change.

<!-- TODO: stack, build/test commands, slice anatomy, naming rules — fill in as specs land. -->

## Release and consumers

Each push to `main` publishes all packages to GitHub Packages as the next preview, `<base>-preview.<n>` (`.github/workflows/publish.yml`). A tag `v<version>` publishes a stable version. After a stable tag, set `<Version>` in `Directory.Build.props` to the next version. Two public repos use the published packages: `vsa-consumer-dotnet` and `vsa-consumer-unity`. After a release, update the Umpire version in each one and make sure that its checks pass. See `docs/consumers.md`.

Fifteen framework projects also target `netstandard2.1`, for Unity: Umpire.Vsa.Domain, Umpire.Messages.Contracts, Umpire.Messages.Handlers, Umpire.Vsa.Contracts.Requests, Umpire.Vsa.Contracts.Events, Umpire.Vsa.Contracts.Commands, Umpire.Vsa.Handlers.Requests, Umpire.Vsa.Handlers.Events, Umpire.Vsa.Handlers.Commands, Umpire.Vsa.Presenters, Umpire.Vsa.Infrastructure, Umpire.Kernel, Umpire.Vsa.Composition, Umpire.Architecture and Umpire.Vsa (`UmpirePortableLayer` in `Directory.Build.props`). A module defines a request, a command, an event or a presenter one time, and the same code works on all hosts. Only the transports (Umpire.Vsa.Hosts.Web, Umpire.Vsa.Views) need ASP.NET Core. For that target, use only APIs of `netstandard2.1`, and the 8.0 `Microsoft.Extensions` packages. The Unity limits, their tests (`tests/Framework.Tests/Unity/`) and the extension points are in `docs/unity.md`.

## Scaffolding: the umpire tool

The `umpire` tool (package `Umpire.Cli`) writes new apps, modules, plugin modules, packs, requests and commands. Each command, except `init` and `update`, writes only new files: it does not write over a file, it does not edit a file, and it stops if one of its files exists. The two exceptions change files: `umpire init` adds the missing setup to an existing app (one list of setup tasks in `tools/cli/Scaffolding/Setup/`, also used by `new app`), and `umpire update` changes only the Umpire versions. Each command is a class in `tools/cli/Commands/` that inherits one base in `Commands/Bases/` (`ToolCommand`, `AppCommand` or `AddCommand`). See "Add a command" in `docs/scaffolding.md`.

```bash
dotnet tool exec Umpire.Cli@<version> -y -- new app <Name>   # global.json, nuget.config, .gitignore, .umpireconfig, <Name>.slnx, AGENTS.md, <Name>.csproj (<Project Sdk="Umpire.Sdk.Web/<version>">), App/, Features/, Plugins/, Tests/
dotnet tool exec Umpire.Cli@<version> -y -- init             # an existing app: adds the missing setup and makes the Umpire SDK the main SDK. --check true writes nothing
dotnet tool exec Umpire.Cli@<version> -y -- add module <Name>                     # the module root, the published domain key, a status request, a presenter, a page and a unit test
dotnet tool exec Umpire.Cli@<version> -y -- add request <Name> -m <Module>        # the request, the response and the handler
dotnet tool exec Umpire.Cli@<version> -y -- add command <Name> -m <Module>        # the command, the result and the handler
dotnet tool exec Umpire.Cli@<version> -y -- add plugin <Name>                     # a plugin module: its root and a base command in Contracts/Commands
dotnet tool exec Umpire.Cli@<version> -y -- add pack <Name>                       # a pack variant in Packs/<Name>: the pack class and its architecture
dotnet tool exec Umpire.Cli@<version> -y -- doctor                                # the version check of the engine (UMP030201), with the tool version
dotnet tool exec Umpire.Cli@<version> -y -- update                                # the Umpire SDK pins and the Umpire.* packages move to the version of the tool
```

The templates are in `tools/cli/Templates/`. They are one app that builds in `Umpire.slnx`, with the sample names `SampleApp`, `SampleModule`, `SampleAction`, `SampleChange`, `SamplePlugin` and `SamplePack`. The tool replaces these names. A template file with `SampleAction` in its path belongs to `add request`. A template file with `SampleChange` in its path belongs to `add command`. A template file with `SamplePlugin` in its path belongs to `add plugin`. A template file with `SamplePack` in its path belongs to `add pack`. Any other file under `Features/SampleModule/`, or with `SampleModule` in its file name, belongs to `add module`. All other files belong to `new app`. `tests/Cli` lists each template file with its command. If you change a template, build `Umpire.slnx` and run the tests in `tests/Cli`. See `docs/scaffolding.md`.
