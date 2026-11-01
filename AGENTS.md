# AGENTS.md

Instructions for AI coding agents (and humans) working in this repository. This file is model-agnostic: do not add tool-specific files such as `CLAUDE.md`; put shared guidance here.

## Project

A Vertical Slice Architecture (VSA) template: base infrastructure, scaffolding, a sample feature, and architecture rules, meant to be reused as the starting point for new projects.

## Layout

| Path                  | Purpose                                                                 |
|-----------------------|-------------------------------------------------------------------------|
| `framework/Layers/`   | The framework code: one project for each layer base. Ships as NuGet packages |
| `framework/Rules/`    | The enforcement: `Sdk/` (VSA.Sdk, the layer references), `Analyzers/` |
| `samples/`            | Sample apps that use the framework as a consumer does. Does not ship   |
| `tests/`              | Tests of the framework itself, with fixtures. Does not ship            |
| `docs/`               | Architecture decisions, conventions, and guides                         |

A module project is under a `Modules/` folder. Its references come from VSA.Sdk, from the folder of the project. Do not add references to a module project by hand: the build fails with `VSA0001`. Inside this repository, the sample projects import VSA.Sdk from source (`samples/VSA.Sdk.Dev.props`). A consumer project is only `<Project Sdk="VSA.Sdk" />`.

## Tests

Run all tests with `dotnet test VSA.slnx`. Run one project with `dotnet test --project tests/Framework.Tests`. The tests use xUnit v3 on Microsoft.Testing.Platform (set in `global.json`).

| Path                     | Put here                                                                  |
|--------------------------|---------------------------------------------------------------------------|
| `tests/Framework.Tests/` | Tests of one framework layer. One folder for each area, for example `Settings/`, `Host/` |
| `tests/Consumer.Tests/`  | Tests that pack the framework and build a copy of a sample as a consumer  |
| `tests/Fixtures/`        | Small modules that tests use. They are not in `VSA.slnx`                  |
| `tests/Shared/`          | Helpers: `Dotnet` (run the CLI), `LocalFeed` (pack), `Workspace` (consumer copy) |

There are two kinds of fixture:

- **Referenced fixture:** a module that must build. A test project references it with `ProjectReference`, and it builds in source mode (`tests/Fixtures/Directory.Build.props`).
- **Built fixture:** a test builds it out of process to check the build result. Write its projects in the consumer form (`<Project Sdk="VSA.Sdk" />`). Use `Workspace.Create(folder)`, then `workspace.Dotnet("build")`.

A new test project goes in `tests/<Name>.Tests/`. Copy the project file of `Framework.Tests` (`OutputType` Exe, `xunit.v3`, `<Using Include="Xunit" />`), and add the project to `VSA.slnx`.

`LocalFeed` packs all framework packages one time for each test process, with version `0.0.0-test`. A `Workspace` restores from that feed with its own NuGet cache, in a short temporary folder.

The public surface of `tests/Shared` (namespace `Tests.Shared`). Other test projects use it, so keep these names stable:

| Member | Use |
|--------|-----|
| `Repo.Root`, `Repo.Path(params string[])` | The repository root, and a path in it |
| `LocalFeed.Path`, `LocalFeed.Version`, `LocalFeed.Packages` | The feed folder (packs on first use), `0.0.0-test`, the package ids. It packs each packable project under `framework/` |
| `Workspace.Create(string copyFrom)` | Copies a folder to `Src` and writes `global.json`, `nuget.config`, `Directory.Build.props` and `Directory.Packages.props` in `Root` |
| `workspace.Root`, `.Src`, `.Env`, `.Dotnet(params string[])`, `.Start(params string[])`, `.Dispose()` | Run dotnet in `Src` with the workspace NuGet cache. `Dispose` deletes the folder |
| `Dotnet.Run(workDir, env, params string[])`, `Dotnet.Start(...)` | Run the dotnet CLI without the MSBuild variables of the test host |
| `DotnetResult(Command, ExitCode, Output)`, `.Succeeded`, `.EnsureSuccess()` | The result of a run |
| `TempFolder.Create()`, `.Copy(from, to)`, `.Delete(path)` | Short temporary folders |

## Core principles

- **Organize by feature, not by layer.** Each slice owns its request, handler, validation, and endpoint in one place.
- **Slices are independent.** A slice does not reach into another slice's internals; share only through explicit, minimal shared code.
- **Keep shared code small.** Promote code to a shared location only when two or more slices genuinely need it.
- **Rules are enforced by tests.** Architecture rules live in `tests/` and must pass.

## Working agreements

- Follow the conventions of the surrounding code (naming, structure, comment density).
- Add or update tests with every behavior change.
- Document notable architecture decisions in `docs/`.
- Keep this file current when conventions change.

<!-- TODO: stack, build/test commands, slice anatomy, naming rules — fill in as specs land. -->
