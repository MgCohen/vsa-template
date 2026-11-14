# AGENTS.md

Instructions for AI coding agents (and humans) working in this repository. This file is model-agnostic: do not add tool-specific files such as `CLAUDE.md`; put shared guidance here.

## Project

A Vertical Slice Architecture (VSA) template: base infrastructure, scaffolding, a sample feature, and architecture rules, meant to be reused as the starting point for new projects.

## Layout

| Path                  | Purpose                                                                 |
|-----------------------|-------------------------------------------------------------------------|
| `framework/Kernel/`   | VSA.Kernel: what each app needs to start, with no layer: `App`, `AppBuilder`, `IAppHost`, `Anchor`, `AnchorBuilder`, `IInstaller`, `AnchorScan`, the factories. `Analyzers/` (VSA.Kernel.Analyzers): the analyzer base `RuleAnalyzer` and the rules about any code. `Markers/` (VSA.Analyzers.Markers): `[ReflectionBase]`, `[AutoRegister]`, `[TypeArgument]` and the IL2CPP markers `[Preserve]`, `[RequireDerived]`, `[RequireImplementors]` |
| `framework/Components/` | Optional blocks that an architecture takes. `Messages/`: `Contracts/` (VSA.Contracts: `IMessage`, `Envelope`, `Result`) and `Handlers/` (VSA.Handlers: the pipeline) |
| `framework/Engine/`   | The architecture engine: `Definition/` (VSA.Architecture, the definition API: `Concept`, `Anchor`, `Part`, `RuleBook`) and `Core/` (VSA.Architecture.Engine: load, bind, decide and check with ArchUnitNET) |
| `framework/Architectures/Vsa/` | The VSA pack: `Definition/` (VSA.Architecture.Vsa: `VsaDefinition`, `Layer`, the VSA layers), `Module/`, the layer bases (`Contracts/`, `Handlers/`, `Domain/`, `Infrastructure/`, `Presenters/`, `Views/`), `Composition/` (`VsaApp`), `Host/`, `Rules/` (VSA.Analyzers: the VSA rules) and `Testing/` (VSA.Testing.Unit) |
| `framework/Sdk/`      | VSA.Sdk: the build setup of each project: the framework references, the analyzers, the rule values and the source folders |
| `framework/Tooling/`  | The `vsa` tool (`Cli/`, package VSA.Cli) and its templates (`Cli/Templates/`) |
| `framework/Tooling/Viewer/` | The architecture viewer: an in-repo VSA app (module `ArchitectureViewer`) with its sample input in `Data/`. Does not ship yet. See `docs/viewer.md` |
| `engine/`             | The rule engine app (a VSA console app: Engine, Definition, Rules, Report, and the Steps plugin). Its root project `engine/Engine.csproj` makes the build hook package VSA.Architecture.Build, with `VSA.Architecture.Build.targets`. See "The rule engine" below |
| `samples/`            | Sample apps that use the framework as a consumer does. Does not ship   |
| `tests/`              | Tests of the framework itself, with fixtures. Does not ship            |
| `docs/`               | Architecture decisions, conventions, and guides                         |

The framework groups and their references are in `docs/framework-layout.md`.

There are three kinds of module: the VSA pack (`framework/Architectures/Vsa/`), a plugin module and a feature module (the code of the app). A feature module is a folder `Features/<Feature>/`, and a plugin module is a folder `Plugins/<Plugin>/`. The code is in `Domain/`, `Domain/Shared/` (the published domain of the module), `Infrastructure/` and `Presenters/`. A feature module has its other code in `Slices/<Slice>/`. A plugin module has no slices: it has the layout of the framework, with one folder for each layer, for example `Handlers/Commands/`. Its root class inherits `Plugin` (`VSA1014`). The word "Shared" is only `Domain/Shared/` and `Slices/Shared/`. See `docs/slice-layout.md`.

An app has one root project in the app root (the root of the definition), for example `samples/Notes/Notes.csproj`. It compiles each file in its folder tree. All other projects are optional, for example a test project or a separate assembly. A parent project does not compile the folder of a nested project, and no project compiles `bin/`, `obj/` or the folders in `VsaExcludedFolders` (for example `Assets/`). The folders select the layers, and the rule engine checks each use. The name of a module is the last part of the namespace of its root class, and one assembly can have many modules.

The app root is the folder of `global.json`. In a Unity project (`ProjectSettings/ProjectVersion.txt` in the app root), the module groups are in `Assets/<AppName>/` and in a sibling folder of `Assets/` (default `Source/`), the views are in `Assets/<AppName>/Views/`, and the architecture checks also read `Library/ScriptAssemblies/` (see `docs/architecture-engine.md`). The Unity build parts are in the `vsa-unity` package (MgCohen/vsa-unity#8). In this repository, `samples/`, `tests/Fixtures/` and the templates set the app root (`VsaAppRoot`) for each app, because the repository has one `global.json`. A plugin module is the base of the same layers in each module, as the framework is. See `docs/architecture-rules.md`.

VSA.Sdk gives each project the base of each layer. Add a VSA library that is not a layer base (for example `VSA.Host`) as a normal reference: a `VSA.*` package with no version gets the version of VSA.Sdk. Inside this repository, the sample projects import VSA.Sdk from source (`samples/VSA.Sdk.Dev.props`). A consumer project is `<Project Sdk="VSA.Sdk" />`, or `<Project Sdk="Microsoft.NET.Sdk.Web">` with `<Sdk Name="VSA.Sdk" />` for a web root project.

Git ignores `VSA.slnx`. To make it, run `dotnet run solution.cs`. The script adds each `.csproj` in the repository, except `tests/Fixtures/`. After you add, move or remove a project, run the script again. Do not edit `VSA.slnx` by hand.

The app has one entry class, `MyApp : VsaApp<TDefinition>` (VSA.Composition, portable, on `App` of VSA.Kernel), in `App/Host/<Name>App.cs`. `TDefinition` is the architecture definition of the app, for example `VsaDefinition`, or `EngineDefinition` in `engine/`. `Program.cs` is only `return new MyApp().Run(args);`. `Run` calls the virtual steps of the app: `Start` and `Build`, and one method for each builder step (`Host()`, `Modules()`, `Keys()`, `Services()`). `Host()` selects the host: the default is `GenericAppHost`, Notes returns `WebVsaHost` (VSA.Host), and the engine returns `ConsoleVsaHost`. There is no host scan. The host decides if `Run` waits or returns. `Stop()` stops the app instance, and `Run` can then start it again. The rule engine reads the definition of the app class. To add app rules, derive a definition from `VsaDefinition`, override `Rules(RuleBook)`, and use it as `TDefinition`. The host finds the modules and the views by itself. To add a feature module, add its files under `Features/`. Do not edit `Program.cs`. Each module root has exactly one class that inherits `Module` or `Module<TSettings>` (`Plugin` or `Plugin<TSettings>` in a plugin module), else startup fails.

`AppBuilder` (VSA.Kernel) is the one entry point. `UseWebHost()` (VSA.Host) adds the HTTP API of the requests and the views, and `UseGenericHost()` is for console apps. A Unity app uses no host: it calls `services.AddVsa(args)` and builds the container itself. `AddVsa` runs the installers, then `Services(...)`, then `Configure` of each anchor (module). `Build()` does the same on the services of the host and returns an `IHost`. A factory (`IFactory<T>`, `services.AddFactory`) makes objects that the container cannot make, for example Unity objects. `AnchorScan` finds the modules, the installers and the views root (`ViewsRoot`) in the loaded assemblies and the `.dll` files of the app. Module code registers services on `IServiceCollection` and resolves them with `IServiceProvider`. These interfaces are container-neutral, so do not add a VSA container interface. The installer of a layer is a class that implements `IInstaller` in the project of that layer, for example `EventHandlerInstaller` in VSA.Handlers.Events. The kernel finds each installer with its scan, so no list of installers changes. To add a kind of registration, write a class with `Install(IServiceCollection services, AnchorTypes types)` in the project of its layer (or add it in `WebVsaHost.Install` if it needs ASP.NET Core). An installer finds its types with `types.Implementations(typeof(TBase))`. See `docs/composition.md`.

Each message goes through a pipeline to its handler (VSA.Handlers). A handler takes `Envelope<TMessage>` and returns `Result<T>`, and each send (`ICommandSender`, `IBus`, `IIntegrationEventPublisher`) returns `Result<T>`. `Result`, `Error` and `Envelope` are in VSA.Contracts, next to `IMessage`: domain code does not use them. A run is: validators, before steps, Before events, the handler, After events, after steps. Validators (`Validator<T>`) and events (`Before<T>`, `After<T>`) are found by type. A module defines a custom pipeline with `services.AddPipeline(new PipelineBuilder<TMessage>()...)`: handlers, runner, steps, all by type. With no definition, each message runs at once (`InlineRunner`). See `docs/handler-pipeline.md`.

A presenter shows the events of its own module: it implements `IPresenterEvent<TEvent>`, and it gets each event, also when no view shows it. A view only renders: it inherits `PresenterWatcher` (one or more presenters) or `PresenterComponent<T>` (one presenter). A presenter does not publish events (`VSA1004`). See `docs/presenter-events.md`.

## The rule engine

The rule engine checks the architecture of an app after each build. It is a VSA console app in `engine/`: its host `ConsoleVsaHost` sends its one entry command, `RunEngineCommand`. Its handler is the composition root, one line for each step: Definition (loads the app and its definition), Rules (checks the layer rules), then Report (writes `file(line,col): error CODE: text` lines). Only `Rules/Infrastructure/` uses ArchUnitNET, and the engine definition (`engine/App/Host/EngineDefinition.cs`) checks that. A step is a feature module with one command that inherits `EngineStep<TResult>` (plugin `engine/Plugins/Steps`). The Steps plugin also has the pipeline parts in `Infrastructure/`: the runner, the before step that skips a step with a missing input, the after step that records the findings, and the step log. A pipeline step is in `Infrastructure/` of a module or of a plugin, and a `Validator<T>` is only in the slice of its handler. To add or remove a step, add or remove its line. The Steps pipeline collects the findings of each step. If a step throws, it records `VSA3000` and the run continues. A step with a missing input (a failed earlier step) is skipped (`VSA3004`). `--failfast true` stops the run at the first throw.

To add a step:

1. Add a feature module `engine/Features/<Step>/` with one slice.
2. Write the command: `public sealed record <Name>Command(...) : EngineStep<<Name>Result>`. Its inputs are the results of earlier steps, for example `LoadedApp App`. A non-nullable input that is `null` skips the step (`VSA3004`).
3. Write the result: `public sealed record <Name>Result(IReadOnlyList<Finding> Findings, ...) : IStepResult`. The Steps pipeline adds its findings to the report.
4. Write the handler: `CommandHandler<<Name>Command, <Name>Result>`. For an expected failure, return an `Error` with a `VSA` code. Do not catch exceptions: the pipeline records them as `VSA3000`.
5. Add one line to `Run` in `RunEngineHandler`: `var x = await commands.SendAsync(new <Name>Command(...), ct);`.
6. Add a case to `tests/Engine.Tests`.

An app installs it with one package on its root project: `<PackageReference Include="VSA.Architecture.Build" PrivateAssets="all" />`. Switches: `-p:VsaCheck=false`, `-p:VsaCheckMode=Warn`. A test project does not check the architecture: only the build of the host does. Run it by hand with `dotnet engine/bin/Debug/net10.0/vsa-engine.dll --root <app root> --bin <host output>`.

## Tests

Run all tests with `dotnet test --solution VSA.slnx`. Run one project with `dotnet test --project tests/Framework.Tests`. The tests use NUnit on Microsoft.Testing.Platform (set in `global.json`). NUnit also runs in Unity (Unity Test Framework), and xUnit does not.

| Path                     | Put here                                                                  |
|--------------------------|---------------------------------------------------------------------------|
| `tests/Framework.Tests/` | Tests of one framework layer. One folder for each area, for example `Settings/`, `Host/` |
| `tests/Consumer.Tests/`  | Tests that pack the framework and build a copy of a sample as a consumer  |
| `tests/Cli/`             | Tests of the `vsa` tool and its templates                                 |
| `tests/Architecture.Tests/` | Tests of the rule engine core, on the fixture apps in `tests/Fixtures/Architecture/` |
| `tests/Composition.Tests/` | Tests of the composer, the installers and `AnchorBuilder`, with no ASP.NET Core |
| `tests/Viewer.Tests/`    | Tests of the architecture viewer: unit and browser tests (category `Browser`, Playwright Chromium, installed by the tests) |
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
3. Else change the rules of the VSA definition (`framework/Architectures/Vsa/Definition/VsaDefinition.cs`). The rule engine (`VSA2001`, `VSA2003`, `VSA2101`, `VSA2102`) checks them. See `docs/architecture-engine.md`.
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

Fifteen framework projects also target `netstandard2.1`, for Unity: VSA.Domain, VSA.Contracts, VSA.Handlers, VSA.Contracts.Requests, VSA.Contracts.Events, VSA.Contracts.Commands, VSA.Handlers.Requests, VSA.Handlers.Events, VSA.Handlers.Commands, VSA.Presenters, VSA.Infrastructure, VSA.Module, VSA.Composition, VSA.Architecture and VSA.Architecture.Vsa (`VsaPortableLayer` in `Directory.Build.props`). A module defines a request, a command, an event or a presenter one time, and the same code works on all hosts. Only the transports (VSA.Host, VSA.Views) need ASP.NET Core. For that target, use only APIs of `netstandard2.1`, and the 8.0 `Microsoft.Extensions` packages. See `docs/consumers.md`.

## Scaffolding: the vsa tool

The `vsa` tool (package `VSA.Cli`) writes new apps, modules and requests. It writes only new files: it does not write over a file, and it does not edit a file. The one exception is `vsa update`: it changes only the VSA versions.

```bash
dotnet tool exec VSA.Cli new app <Name>          # global.json, nuget.config, .gitignore, dotnet-tools.json, <Name>.slnx, AGENTS.md, <Name>.csproj, App/, Features/, Plugins/, Tests/
dotnet vsa add module <Name>                     # the module root, a status request, a presenter and a page
dotnet vsa add request <Name> -m <Module>        # the request, the response and the handler
dotnet vsa add command <Name> -m <Module>        # the command, the result and the handler
dotnet vsa add plugin <Name>                     # a plugin module: its root and a base command in Contracts/Commands
dotnet vsa doctor                                # the tool, VSA.Sdk and the VSA.* packages have one version
dotnet vsa update                                # the tool, VSA.Sdk and the VSA.* packages move to the latest version
```

The templates are in `framework/Tooling/Cli/Templates/`. They are one app that builds in `VSA.slnx`, with the sample names `SampleApp`, `SampleModule`, `SampleAction`, `SampleChange` and `SamplePlugin`. The tool replaces these names. A template file with `SampleAction` in its path belongs to `add request`. A template file with `SampleChange` in its path belongs to `add command`. A template file with `SamplePlugin` in its path belongs to `add plugin`. Any other file under `Features/SampleModule/`, or with `SampleModule` in its file name, belongs to `add module`. All other files belong to `new app`. `tests/Cli` lists each template file with its command. If you change a template, build `VSA.slnx` and run the tests in `tests/Cli`. See `docs/scaffolding.md`.
