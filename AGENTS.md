# AGENTS.md

Instructions for AI coding agents (and humans) working in this repository. This file is model-agnostic: do not add tool-specific files such as `CLAUDE.md`; put shared guidance here.

## Project

A Vertical Slice Architecture (VSA) template: base infrastructure, scaffolding, a sample feature, and architecture rules, meant to be reused as the starting point for new projects.

## Layout

| Path                  | Purpose                                                                 |
|-----------------------|-------------------------------------------------------------------------|
| `framework/Kernel/`   | VSA.Kernel: what each app needs to start, with no layer: `App`, `App<TPack>`, `IApp`, `Pack`, `AppHost`, `Module`, `AppTypes`, the factories. `Analyzers/` (VSA.Kernel.Analyzers): the analyzer base `RuleAnalyzer` and the rules about any code. `Markers/` (VSA.Analyzers.Markers): `[ReflectionBase]`, `[AutoRegister]`, `[TypeArgument]` and the IL2CPP markers `[Preserve]`, `[RequireDerived]`, `[RequireImplementors]` |
| `framework/Components/` | Optional blocks that an architecture takes. `Messages/`: `Contracts/` (VSA.Contracts: `IMessage`, `Envelope`, `Result`) and `Handlers/` (VSA.Handlers: the pipeline) |
| `framework/Engine/`   | The architecture engine: `Definition/` (VSA.Architecture, the definition API: `Architecture`, `Concept`, `Module`, `Part`, `PartGroup`, `RuleBook`) `Core/` (VSA.Architecture.Engine: load, bind, decide and check with ArchUnitNET) and `Versions/` (VSA.Architecture.Versions: the version check `VSA3001`, also in the `vsa` tool) |
| `framework/Architectures/Vsa/` | The VSA pack: `Definition/` (VSA.Architecture.Vsa: `VsaArchitecture`, `Layer`, the VSA layers in `Concepts/`, `Vsa<THost>`), the layer bases (`Contracts/`, `Handlers/`, `Domain/`, `Infrastructure/`, `Presenters/`, `Views/`), `Composition/` (`VsaHost`, `ConsoleVsaHost`, `Plugin`), `Host/` (`WebVsaHost`), `Rules/` (VSA.Analyzers: the VSA rules) and `Testing/` (VSA.Testing.Unit) |
| `framework/Sdk/`      | VSA.Sdk: the build setup of each project: the framework references, the analyzers, the rule values and the source folders |
| `framework/Tooling/`  | The `vsa` tool (`Cli/`, package VSA.Cli) and its templates (`Cli/Templates/`) |
| `engine/`             | The rule engine app (a VSA console app: Engine, Definition, Rules, Report, and the Steps plugin). Its root project `engine/Engine.csproj` makes the build hook package VSA.Architecture.Build, with `VSA.Architecture.Build.targets`. See "The rule engine" below |
| `samples/`            | Sample apps that use the framework as a consumer does. Does not ship   |
| `tests/`              | Tests of the framework itself, with fixtures. Does not ship            |
| `docs/`               | Architecture decisions, conventions, and guides                         |

The framework groups and their references are in `docs/framework-layout.md`.

There are three kinds of module: the VSA pack (`framework/Architectures/Vsa/`), a plugin module and a feature module (the code of the app). A feature module is a folder `Features/<Feature>/`, and a plugin module is a folder `Plugins/<Plugin>/`. The code is in `Domain/`, `Domain/Shared/` (the published domain of the module), `Infrastructure/` and `Presenters/`. A feature module has its other code in `Slices/<Slice>/`. A plugin module has no slices: it has the layout of the framework, with one folder for each layer, for example `Handlers/Commands/`. Its root class inherits `Plugin` (`VSA1014`). The word "Shared" is only `Domain/Shared/` and `Slices/Shared/`. See `docs/slice-layout.md`.

An app has one root project in the app root (the root of the definition), for example `samples/Notes/Notes.csproj`. It compiles each file in its folder tree. All other projects are optional, for example a test project or a separate assembly. A parent project does not compile the folder of a nested project, and no project compiles `bin/`, `obj/` or the folders in `VsaExcludedFolders` (for example `Assets/`). The folders select the layers, and the rule engine checks each use. The name of a module is the last part of the namespace of its root class, and one assembly can have many modules. Each namespace starts with the app name and follows the folders, for example `Notes.Features.Notebook.Slices.CreateNote` or `Notes.App.Host`. The `using` directives go inside the namespace (`IDE0065`), so a type of VSA wins over a namespace of the app with the same name, for example `App`.

The app root is the folder of `global.json`. In a Unity project (`ProjectSettings/ProjectVersion.txt` in the app root), the module groups are in `Assets/<AppName>/` and in a sibling folder of `Assets/` (default `Source/`), the views are in `Assets/<AppName>/Views/`, and the architecture checks also read `Library/ScriptAssemblies/` (see `docs/architecture-engine.md`). The Unity build parts are in the `vsa-unity` package (MgCohen/vsa-unity#8). In this repository, `samples/`, `tests/Fixtures/` and the templates set the app root (`VsaAppRoot`) for each app, because the repository has one `global.json`. A plugin module is the base of the same layers in each module, as the framework is. See `docs/architecture-rules.md`.

VSA.Sdk gives each project the base of each layer. Add a VSA library that is not a layer base (for example `VSA.Host`) as a normal reference, with the version of VSA.Sdk. The rule engine checks each VSA version (`VSA3001`). Inside this repository, the sample projects import VSA.Sdk from source (`samples/VSA.Sdk.Dev.props`). A consumer project is `<Project Sdk="VSA.Sdk" />`, or `<Project Sdk="Microsoft.NET.Sdk.Web">` with `<Sdk Name="VSA.Sdk" />` for a web root project.

Git ignores `VSA.slnx`. To make it, run `dotnet run solution.cs`. The script adds each `.csproj` in the repository, except `tests/Fixtures/`. After you add, move or remove a project, run the script again. Do not edit `VSA.slnx` by hand.

The app has one entry class, `MyApp : App<TPack>` (VSA.Kernel, portable), in `App/Host/<Name>App.cs`. The pack joins the architecture and the host: Notes is `App<Vsa<WebVsaHost>>`, and the engine is `App<Vsa<EngineArchitecture, ConsoleVsaHost>>`. `Vsa<THost>` (VSA.Architecture.Vsa) uses `VsaArchitecture`. The app class file imports `VSA.Kernel`, `VSA.Architecture.Vsa` and the namespace of the host. `App<…>` is not ambiguous with the record `App`, but `App` with no type argument is. `Program.cs` is only `return new MyApp().Run(args);`. The app class can override `ConfigureServices` (last), `OnStart`, `OnStop`, `Modules()` and `Keys()`. Each `Run` makes a new host from the pack. The host decides if `Run` waits or returns. `Stop()` stops the app instance, and `Run` can then start it again. The rule engine reads `Pack.Architecture` of the app class (`VSA3003` if the app has no app class). To add app rules, derive an architecture from `VsaArchitecture`, override `Rules(RuleBook)`, and use `Vsa<TArchitecture, THost>`. The host finds the modules and the views by itself. To add a feature module, add its files under `Features/`. Do not edit `Program.cs`. Each module root has exactly one class that inherits `Module` or `Module<TSettings>` (`Plugin` or `Plugin<TSettings>` in a plugin module), else startup fails.

The host (`AppHost`, VSA.Kernel) is the composition root. `AppHost.Compose(services, app)` has a fixed order: the core (`IConfiguration`, logging, `AppTypes`, auto-register), then `Install(services, types)` of the host, then `Configure(IServiceCollection)` of each module in `Order`, then `app.ConfigureServices`. `VsaHost` installs each VSA layer by a direct call, and `WebVsaHost` (VSA.Host) also adds the HTTP API of the requests and the views. There is no installer scan. A Unity app with its own container calls `new Vsa<UnityVsaHost>().CreateHost().Compose(services, app)` and builds the container itself. A module replaces a default with `services.Replace<TService, TImplementation>()`. A factory (`IFactory<T>`, `services.AddFactory`) makes objects that the container cannot make, for example Unity objects. `AppTypes` finds the modules and the views root (`ViewsRoot`) in the loaded assemblies and the `.dll` files of the app. Module code registers services on `IServiceCollection` and resolves them with `IServiceProvider`. These interfaces are container-neutral, so do not add a VSA container interface. To add a kind of registration, write a public `Add<Name>(this IServiceCollection services, AppTypes types)` extension in the project of its layer, and call it in `VsaHost.Install` (or in `WebVsaHost.Install` if it needs ASP.NET Core). It finds its types with `types.Implementations(typeof(TBase))`. See `docs/composition.md`.

Each message goes through a pipeline to its handler (VSA.Handlers). A handler takes `Envelope<TMessage>` and returns `Result<T>`, and each send (`ICommandSender`, `IBus`, `IIntegrationEventPublisher`) returns `Result<T>`. `Result`, `Error` and `Envelope` are in VSA.Contracts, next to `IMessage`: domain code does not use them. A run is: validators, before steps, Before hooks, the handler, After hooks, after steps. Validators (`Validator<T>`) are found by type, and each is only in the slice of its handler. A hook (`BeforeCommandHook<T>`, `AfterCommandHook<T, TResult>`, `BeforeEventHook<T>`, `AfterEventHook<T>`) is in its own slice, and its name ends with `Hook` (`VSA1015`). No hook runs for a request. A step (`BeforeStep`, `AfterStep`, or the typed `BeforeStep<T>`, `AfterStep<T>`) belongs to one pipeline. A module defines a custom pipeline with `services.AddPipeline(new PipelineBuilder<TMessage>()...)`: handlers, hook bases (`HooksOf`), runner, steps, all by type. With no definition, each message runs at once (`InlineRunner`). See `docs/handler-pipeline.md`.

A presenter shows the events of its own module: it implements `IPresenterEvent<TEvent>`, and it gets each event, also when no view shows it. A view only renders: it inherits `PresenterWatcher` (one or more presenters) or `PresenterComponent<T>` (one presenter). A presenter does not publish events (`VSA1004`). See `docs/presenter-events.md`.

## The rule engine

The rule engine checks the architecture of an app after each build. It is a VSA console app in `engine/`: its host `ConsoleVsaHost` sends its one entry command, `RunEngineCommand`. Its handler is the composition root, one line for each step: Versions (checks that the app and the engine have one VSA version, `VSA3001`), Definition (loads the app and its definition), Rules (checks the layer rules), Baseline (removes the errors that `vsa-baseline.txt` in the app root accepts), then Report (sorts the findings and writes `file(line,col): error CODE: text` lines). Only `Rules/Infrastructure/` uses ArchUnitNET, and the engine definition (`engine/App/Host/EngineArchitecture.cs`) checks that. A step is a feature module with one command that inherits `EngineStep<TResult>` (plugin `engine/Plugins/Steps`). The Steps plugin also has the pipeline parts in `Infrastructure/`: the runner, the before step that skips a step with a missing input, the after step that records the findings, and the step log. A pipeline step is in `Infrastructure/` of a module or of a plugin, and a `Validator<T>` is only in the slice of its handler. To add or remove a step, add or remove its line. The Steps pipeline collects the findings of each step. If a step throws, it records `VSA3000` and the run continues. A step with a missing input (a failed earlier step) is skipped (`VSA3004`). `--failfast true` stops the run at the first throw.

To add a step:

1. Add a feature module `engine/Features/<Step>/` with one slice.
2. Write the command: `public sealed record <Name>Command(...) : EngineStep<<Name>Result>`. Its inputs are the results of earlier steps, for example `AppInput App`. A non-nullable input that is `null` skips the step (`VSA3004`).
3. Write the result: `public sealed record <Name>Result(IReadOnlyList<Finding> Findings, ...) : IStepResult`. The Steps pipeline adds its findings to the report.
4. Write the handler: `CommandHandler<<Name>Command, <Name>Result>`. For an expected failure, return an `Error` with a `VSA` code. Do not catch exceptions: the pipeline records them as `VSA3000`.
5. Add one line to `Run` in `RunEngineHandler`: `var x = await commands.SendAsync(new <Name>Command(...), ct);`.
6. Add a case to `tests/Engine.Tests`.

An app installs it with one package on its root project: `<PackageReference Include="VSA.Architecture.Build" PrivateAssets="all" />`. Switches: `-p:VsaCheck=false`, `-p:VsaCheckMode=Warn`, `-p:VsaUpdateBaseline=true` (writes the errors of the build to `vsa-baseline.txt`, see `docs/architecture-engine.md`). A test project does not check the architecture: only the build of the host does. Run it by hand with `dotnet engine/bin/Debug/net10.0/vsa-engine.dll --root <app root> --bin <host output>`.

## Tests

Run all tests with `dotnet test --solution VSA.slnx`. Run one project with `dotnet test --project tests/Framework.Tests`. The tests use NUnit on Microsoft.Testing.Platform (set in `global.json`). NUnit also runs in Unity (Unity Test Framework), and xUnit does not.

| Path                     | Put here                                                                  |
|--------------------------|---------------------------------------------------------------------------|
| `tests/Framework.Tests/` | Tests of one framework layer. One folder for each area, for example `Settings/`, `Host/` |
| `tests/Consumer.Tests/`  | Tests that pack the framework and build a copy of a sample as a consumer  |
| `tests/Cli/`             | Tests of the `vsa` tool and its templates                                 |
| `tests/Architecture.Tests/` | Tests of the rule engine core, on the fixture apps in `tests/Fixtures/Architecture/` |
| `tests/Composition.Tests/` | Tests of `Compose`, the layer registrations and the console host, with no ASP.NET Core |
| `tests/Engine.Tests/`    | Tests of the rule engine app, in the test process, on `samples/Notes`     |
| `tests/Fixtures/`        | Small modules that tests use. They are not in `VSA.slnx`                  |
| `tests/Shared/`          | Helpers: `Dotnet` (run the CLI), `LocalFeed` (pack), `Workspace` (consumer copy) |

There are two kinds of fixture:

- **Referenced fixture:** a module that must build. A test project references it with `ProjectReference`, and it builds in source mode (`tests/Fixtures/Directory.Build.props`).
- **Built fixture:** a test builds it out of process to check the build result. Write its projects in the consumer form (`<Project Sdk="VSA.Sdk" />`). Use `Workspace.Create(folder)`, then `workspace.Dotnet("build")`.

A new test project goes in `tests/<Name>.Tests/`, then run `dotnet run solution.cs` to add it to `VSA.slnx`. Its project file has only its own references. `tests/Directory.Build.props` gives each project whose name ends with `.Tests` the NUnit settings: `OutputType` Exe, `EnableNUnitRunner`, the NUnit packages and `using NUnit.Framework`.

`LocalFeed` packs all framework packages one time for each test process, with version `0.0.0-test`. A `Workspace` restores from that feed with its own NuGet cache, in a short temporary folder.

The public surface of `tests/Shared` (namespace `Tests.Shared`). Other test projects use it, so keep these names stable:

| Member | Use |
|--------|-----|
| `Repo.Root`, `Repo.Path(params string[])` | The repository root, and a path in it |
| `LocalFeed.Path`, `LocalFeed.Version`, `LocalFeed.Packages` | The feed folder (packs on first use), `0.0.0-test`, the package ids. It packs each packable project under `framework/` |
| `Workspace.Create(string copyFrom)` | Copies a folder to `Src` and writes `global.json` in `Src` (the app root), and `nuget.config`, `Directory.Build.props` and `Directory.Packages.props` in `Root` |
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

1. If a built-in .NET rule does the check, set its severity in `framework/Sdk/Sdk/VSA.globalconfig`.
2. If one file shows the problem, write a rule class that inherits `VsaAnalyzerRule`: a VSA rule in `framework/Architectures/Vsa/Rules/`, a rule about any code in `framework/Kernel/Analyzers/`. The analyzer of that DLL (`VsaAnalyzer` or `KernelAnalyzer`, both on `RuleAnalyzer`) finds and runs it. Use the next free `VSA1xxx` ID, and add it to `AnalyzerReleases.Unshipped.md`. Each value that a rule uses (a limit, a list, an order) is an option: list it in `RequiredOptions`, write its value only in `framework/Sdk/Sdk/VSA.Rules.globalconfig`, and read it with `Options(...)`. A rule has no default value in code: a missing value gives `VSA1000`. A rule with a maximum inherits `LimitRule`. A rule about any code, not only modules, sets `IsModuleRule` to false: then it also checks the framework code.
3. Else change the rules of the VSA definition (`framework/Architectures/Vsa/Definition/VsaArchitecture.cs`). The rule engine (`VSA2001`, `VSA2003`, `VSA2101`, `VSA2102`) checks them. See `docs/architecture-engine.md`.
4. Write a message that gives what is wrong, the type names, and the fix.
5. Add tests in `tests/Rules.Tests/`. For an analyzer, use the Roslyn analyzer test library (`AnalyzerTests.cs`). For the architecture engine, add a case to `tests/Architecture.Tests/` with the fixture apps in `tests/Fixtures/Architecture/`.
6. Add the rule to the table in `docs/architecture-rules.md`.

## Working agreements

- Follow the conventions of the surrounding code (naming, structure).
- Do not write comments in C# code (`VSA1013`). Put the information in a name: a type, a member, a variable or a test.
- Both namespace forms are allowed (block and file-scoped). A MonoBehaviour must use a block namespace in Unity.
- Add or update tests with every behavior change.
- Document notable architecture decisions in `docs/`.
- Keep this file current when conventions change.

<!-- TODO: stack, build/test commands, slice anatomy, naming rules — fill in as specs land. -->

## Release and consumers

Each push to `main` publishes all packages to GitHub Packages as the next preview, `<base>-preview.<n>` (`.github/workflows/publish.yml`). A tag `v<version>` publishes a stable version. After a stable tag, set `<Version>` in `Directory.Build.props` to the next version. Two public repos use the published packages: `vsa-consumer-dotnet` and `vsa-consumer-unity`. After a release, update the VSA version in each one and make sure that its checks pass. See `docs/consumers.md`.

Fifteen framework projects also target `netstandard2.1`, for Unity: VSA.Domain, VSA.Contracts, VSA.Handlers, VSA.Contracts.Requests, VSA.Contracts.Events, VSA.Contracts.Commands, VSA.Handlers.Requests, VSA.Handlers.Events, VSA.Handlers.Commands, VSA.Presenters, VSA.Infrastructure, VSA.Kernel, VSA.Composition, VSA.Architecture and VSA.Architecture.Vsa (`VsaPortableLayer` in `Directory.Build.props`). A module defines a request, a command, an event or a presenter one time, and the same code works on all hosts. Only the transports (VSA.Host, VSA.Views) need ASP.NET Core. For that target, use only APIs of `netstandard2.1`, and the 8.0 `Microsoft.Extensions` packages. See `docs/consumers.md`.

## Scaffolding: the vsa tool

The `vsa` tool (package `VSA.Cli`) writes new apps, modules, plugin modules, requests and commands. It writes only new files: it does not write over a file, and it does not edit a file. The one exception is `vsa update`: it changes only the VSA versions. Each command is a class in `framework/Tooling/Cli/Commands/` that inherits one base in `Commands/Bases/` (`ToolCommand`, `AppCommand` or `AddCommand`). See "Add a command" in `docs/scaffolding.md`.

```bash
dotnet tool exec VSA.Cli new app <Name>          # global.json, nuget.config, .gitignore, dotnet-tools.json, <Name>.slnx, AGENTS.md, <Name>.csproj, App/, Features/, Plugins/, Tests/
dotnet vsa add module <Name>                     # the module root, the published domain key, a status request, a presenter, a page and a unit test
dotnet vsa add request <Name> -m <Module>        # the request, the response and the handler
dotnet vsa add command <Name> -m <Module>        # the command, the result and the handler
dotnet vsa add plugin <Name>                     # a plugin module: its root and a base command in Contracts/Commands
dotnet vsa doctor                                # the version check of the engine (VSA3001), with the tool version
dotnet vsa update                                # the tool, VSA.Sdk and the VSA.* packages move to the latest version
```

The templates are in `framework/Tooling/Cli/Templates/`. They are one app that builds in `VSA.slnx`, with the sample names `SampleApp`, `SampleModule`, `SampleAction`, `SampleChange` and `SamplePlugin`. The tool replaces these names. A template file with `SampleAction` in its path belongs to `add request`. A template file with `SampleChange` in its path belongs to `add command`. A template file with `SamplePlugin` in its path belongs to `add plugin`. Any other file under `Features/SampleModule/`, or with `SampleModule` in its file name, belongs to `add module`. All other files belong to `new app`. `tests/Cli` lists each template file with its command. If you change a template, build `VSA.slnx` and run the tests in `tests/Cli`. See `docs/scaffolding.md`.
