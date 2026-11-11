# AGENTS.md

Instructions for AI coding agents (and humans) working in this repository. This file is model-agnostic: do not add tool-specific files such as `CLAUDE.md`; put shared guidance here.

## Project

A Vertical Slice Architecture (VSA) template: base infrastructure, scaffolding, a sample feature, and architecture rules, meant to be reused as the starting point for new projects.

## Layout

| Path                  | Purpose                                                                 |
|-----------------------|-------------------------------------------------------------------------|
| `framework/Core/`     | The framework core: one project for each layer base. Ships as NuGet packages. Plugin modules extend it |
| `framework/Sdk/`      | VSA.Sdk: the build setup of each module project, the layer references and the slice router (`Tasks/`) |
| `framework/Analyzers/`| VSA.Analyzers: the Roslyn rules that run in the editor and the build. `Markers/` (VSA.Analyzers.Markers): the attributes that the rules read, for example `[ReflectionBase]` |
| `framework/Testing/`  | The test bases that ship: `Core/` (the layer map), `Architecture/` (ArchUnitNET), `Unit/` (NUnit, NSubstitute) |
| `framework/Tooling/`  | The `vsa` tool (`Cli/`, package VSA.Cli) and its templates (`Cli/Templates/`) |
| `samples/`            | Sample apps that use the framework as a consumer does. Does not ship   |
| `tests/`              | Tests of the framework itself, with fixtures. Does not ship            |
| `docs/`               | Architecture decisions, conventions, and guides                         |

A module is a folder `Modules/<Module>/`, and a plugin module is a folder `Plugins/<Module>/`. The root project is `<Module>/<Module>.csproj`, and the layer projects are in `<Module>/Projects/`, named `<Module>.<Layer>`. The code is in `Domain/`, `Domain/Shared/` (the published domain of the module), `Infrastructure/` and `Presenters/`. A module has its other code in `Slices/<Feature>/`: the type in each file selects its project. A plugin module has no slices: it has the layout of the framework, with one folder for each layer, for example `Handlers/Commands/`. Its root class inherits `Plugin` (`VSA1014`). The word "Shared" is only `Domain/Shared/` and `Slices/Shared/`. VSA.Sdk gives each project its files and its references. See `docs/slice-layout.md`.

The app root is the folder of `global.json`. In this repository, `samples/`, `tests/Fixtures/` and the templates set the app root (`VsaAppRoot`) for each app, because the repository has one `global.json`. A plugin module is the base of the same layers in each module, as the framework is. See `docs/architecture-rules.md`.

Do not add references or `Compile` items to a module project by hand: the build fails with `VSA0001` or `VSA0006`. Inside this repository, the sample projects import VSA.Sdk from source (`samples/VSA.Sdk.Dev.props`). A consumer project is only `<Project Sdk="VSA.Sdk" />`.

Git ignores `VSA.slnx`. To make it, run `dotnet run solution.cs`. The script adds each `.csproj` in the repository, except `tests/Fixtures/`. After you add, move or remove a project, run the script again. Do not edit `VSA.slnx` by hand.

The host finds the modules and the views by itself: `Program.cs` calls `VsaBuilder.Create(args).UseWebHost().Build().RunAsync()`. To add a module, add its files under `Modules/`. Do not edit `Program.cs`. Each module root has exactly one class that inherits `Module` or `Module<TSettings>` (`Plugin` or `Plugin<TSettings>` in a plugin module), else startup fails.

`VsaBuilder` (VSA.Composition) is the one entry point. `UseWebHost()` (VSA.Host) adds the HTTP API of the requests and the views, and `UseGenericHost()` is for console apps. A Unity app uses no host: it calls `services.AddVsa(args)` and builds the container itself. `AddVsa` runs the layer installers, then `Services(...)`, then `Module.Configure` of each module. `Build()` does the same on the services of the host and returns an `IHost`. A factory (`IFactory<T>`, `services.AddFactory`) makes objects that the container cannot make, for example Unity objects. `AppScan` finds the modules and the views root (`ViewsRoot`) in the loaded assemblies and the `.dll` files of the app. Module code registers services on `IServiceCollection` and resolves them with `IServiceProvider`. These interfaces are container-neutral, so do not add a VSA container interface. The installer of a layer is an internal static class in the project of that layer, for example `EventHandlerInstaller` in VSA.Handlers.Events. To add a kind of registration, write `Install(IServiceCollection services, ModuleTypes types)` in the project of its layer, and call it from `VsaArchitecture.InstallLayers` (or from `WebVsaHost.Install` if it needs ASP.NET Core). An installer finds its types with `types.Implementations(typeof(TBase))`. See `docs/composition.md`.

Each message goes through a pipeline to its handler (VSA.Handlers). A handler takes `Envelope<TMessage>` and returns `Result<T>`, and each send (`ICommandSender`, `IBus`, `IIntegrationEventPublisher`) returns `Result<T>`. `Result`, `Error` and `Envelope` are in VSA.Contracts, next to `IMessage`: domain code does not use them. A run is: validators, before steps, Before events, the handler, After events, after steps. Validators (`Validator<T>`) and events (`Before<T>`, `After<T>`) are found by type. A module defines a custom pipeline with `services.AddPipeline(new PipelineBuilder<TMessage>()...)`: handlers, runner, steps, all by type. With no definition, each message runs at once (`InlineRunner`). See `docs/handler-pipeline.md`.

A presenter shows the events of its own module: it implements `IPresenterEvent<TEvent>`, and it gets each event, also when no view shows it. A view only renders: it inherits `PresenterWatcher` (one or more presenters) or `PresenterComponent<T>` (one presenter). A presenter does not publish events (`VSA1004`). See `docs/presenter-events.md`.

## Tests

Run all tests with `dotnet test --solution VSA.slnx`. Run one project with `dotnet test --project tests/Framework.Tests`. The tests use NUnit on Microsoft.Testing.Platform (set in `global.json`). NUnit also runs in Unity (Unity Test Framework), and xUnit does not.

| Path                     | Put here                                                                  |
|--------------------------|---------------------------------------------------------------------------|
| `tests/Framework.Tests/` | Tests of one framework layer. One folder for each area, for example `Settings/`, `Host/` |
| `tests/Consumer.Tests/`  | Tests that pack the framework and build a copy of a sample as a consumer  |
| `tests/Cli/`             | Tests of the `vsa` tool and its templates                                 |
| `tests/Composition.Tests/` | Tests of the composer, the installers and `ModuleBuilder`, with no ASP.NET Core |
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
| `TempFolder.Create()`, `.Copy(from, to)`, `.Delete(path)` | Short temporary folders |

## Core principles

- **Organize by feature, not by layer.** Each slice is one folder, `Slices/<Feature>/`, with its request, response, events, handler and helpers. A slice has one kind of handler: a request handler, an event handler or a command handler.
- **Slices are independent.** A slice uses only its own types and `Slices/Shared/`. An event is in the slice that publishes it.
- **Modules talk with commands and events.** A command goes to one handler of another module. Name the types `<Name>Command`, `<Name>Result` and `<Name>Handler`. See `docs/slice-layout.md`.
- **Keep shared code small.** Promote code to a shared location only when two or more slices genuinely need it.
- **Rules are enforced by tests.** Architecture rules live in `tests/` and must pass.

## Add a check

The levels, rules and base classes are in `docs/architecture-rules.md`. Use a library first: the built-in .NET analyzers, Roslyn, ArchUnitNET or NUnit. Tests use NUnit, so they also run in Unity.

1. If a built-in .NET rule does the check, set its severity in `framework/Sdk/Sdk/VSA.globalconfig`.
2. If one file shows the problem, write a rule class that inherits `VsaAnalyzerRule` in `framework/Analyzers/`. The one `VsaAnalyzer` finds and runs it. Use the next free `VSA1xxx` ID, and add it to `AnalyzerReleases.Unshipped.md`. Each value that a rule uses (a limit, a list, an order) is an option: list it in `RequiredOptions`, write its value only in `framework/Sdk/Sdk/VSA.Rules.globalconfig`, and read it with `Options(...)`. A rule has no default value in code: a missing value gives `VSA1000`. A rule with a maximum inherits `LimitRule`. A rule about any code, not only modules, sets `IsModuleRule` to false: then it also checks the framework code.
3. Else add one `[Test]` with an ArchUnitNET rule to `VsaArchitectureTests` (`framework/Testing/Architecture/`). Write the rule only there. Use the next free `VSA2xxx` ID in the test name. For a layer rule, change the layer map in `framework/Testing/Core/Layers.cs` and in `VSA.Layers.targets`.
4. Write a message that gives what is wrong, the type names, and the fix.
5. Add tests in `tests/Rules.Tests/`. For a build rule (`VSA0xxx`), add a case to `BuildRuleTests.cs` (built fixture `tests/Fixtures/Built/Shop`). For an analyzer, use the Roslyn analyzer test library (`AnalyzerTests.cs`). For an architecture rule, add a good and a bad fixture module in `tests/Fixtures/Rules/Modules/`, and call the base test with `FixtureApp.Of("<module>")`.
6. Add the rule to the table in `docs/architecture-rules.md`.

## Working agreements

- Follow the conventions of the surrounding code (naming, structure).
- Do not write comments in C# code (`VSA1013`). Put the information in a name: a type, a member, a variable or a test.
- Add or update tests with every behavior change.
- Document notable architecture decisions in `docs/`.
- Keep this file current when conventions change.

<!-- TODO: stack, build/test commands, slice anatomy, naming rules — fill in as specs land. -->

## Release and consumers

Each push to `main` publishes all packages to GitHub Packages as the next preview, `<base>-preview.<n>` (`.github/workflows/publish.yml`). A tag `v<version>` publishes a stable version. After a stable tag, set `<Version>` in `Directory.Build.props` to the next version. Two public repos use the published packages: `vsa-consumer-dotnet` and `vsa-consumer-unity`. After a release, update the VSA version in each one and make sure that its checks pass. See `docs/consumers.md`.

Thirteen framework projects also target `netstandard2.1`, for Unity: VSA.Domain, VSA.Contracts, VSA.Handlers, VSA.Contracts.Requests, VSA.Contracts.Events, VSA.Contracts.Commands, VSA.Handlers.Requests, VSA.Handlers.Events, VSA.Handlers.Commands, VSA.Presenters, VSA.Infrastructure, VSA.Module and VSA.Composition (`VsaPortableLayer` in `Directory.Build.props`). A module defines a request, a command, an event or a presenter one time, and the same code works on all hosts. Only the transports (VSA.Host, VSA.Views) need ASP.NET Core. For that target, use only APIs of `netstandard2.1`, and the 8.0 `Microsoft.Extensions` packages. See `docs/consumers.md`.

## Scaffolding: the vsa tool

The `vsa` tool (package `VSA.Cli`) writes new apps, modules and requests. It writes only new files: it does not write over a file, and it does not edit a file, except with `dotnet sln add`. The one exception is `vsa update`: it changes only the VSA versions.

```bash
dotnet tool exec VSA.Cli new app <Name>          # global.json, nuget.config, .gitignore, dotnet-tools.json, <Name>.slnx, AGENTS.md, App/, Modules/, Plugins/
dotnet vsa add module <Name>                     # all layers, a status request, a presenter and a page
dotnet vsa add request <Name> -m <Module>        # the request, the response and the handler
dotnet vsa add command <Name> -m <Module>        # the command, the result and the handler; the command projects if they are missing
dotnet vsa add plugin <Name>                     # a plugin module: its root and a base command in Contracts/Commands
dotnet vsa doctor                                # the tool, VSA.Sdk and the VSA.* packages have one version
dotnet vsa update                                # the tool, VSA.Sdk and the VSA.* packages move to the latest version
```

The templates are in `framework/Tooling/Cli/Templates/`. They are one app that builds in `VSA.slnx`, with the sample names `SampleApp`, `SampleModule`, `SampleAction`, `SampleChange` and `SamplePlugin`. The tool replaces these names. A template file with `SampleAction` in its path belongs to `add request`. A template file with `SampleChange` in its path, and the two projects `SampleModule.Contracts.Commands` and `SampleModule.Handlers.Commands`, belong to `add command`. A template file with `SamplePlugin` in its path belongs to `add plugin`. Any other file under `Modules/SampleModule/`, or with `SampleModule` in its file name, belongs to `add module`. All other files belong to `new app`. `tests/Cli` lists each template file with its command. If you change a template, build `VSA.slnx` and run the tests in `tests/Cli`. See `docs/scaffolding.md`.
