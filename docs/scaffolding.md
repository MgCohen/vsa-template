# Scaffolding: the vsa tool

The `vsa` tool (package `VSA.Cli`) writes new apps, modules, plugin modules, requests and commands from templates. The templates are one compilable app in this repository, so `VSA.slnx` builds them as normal code (`dotnet run solution.cs` makes `VSA.slnx`). The tool, VSA.Sdk and the VSA.* packages have one version, `vsa doctor` finds a mismatch, and `vsa update` moves all to the latest version.

## Commands

| Command | Writes | Edits |
|---|---|---|
| `new app <Name>` | Root files, `App/Host`, `App/Views`, `Tests/`, `Features/`, `Plugins/` | None |
| `add module <Name>` | Layers, published domain, status request, presenter, page, unit test | Solution, with `dotnet sln add` |
| `add request <Name> -m <Module>` | Request, response, handler | None |
| `add command <Name> -m <Module>` | Command, result, handler. If the module has no command projects: `Contracts.Commands` and `Handlers.Commands` | Solution, with `dotnet sln add`, if the tool writes the command projects |
| `add plugin <Name>` | Plugin root (inherits `Plugin`), `Contracts.Commands` with the base command `<Name>Step<TResult>` | Solution, with `dotnet sln add` |
| `doctor` | None | None |
| `update [--version <version>]` | None | The tool manifest, with `dotnet tool update`. Then the VSA versions in `global.json` and the project files |

Rules for all commands:

- A name is a PascalCase C# identifier. Names that start with `VSA` are forbidden. These names are also forbidden, because they break the build: `App`, `Views`, `Pages`, `Home`, `Features`, `Plugins`, `System`, `Microsoft`.
- A name cannot contain a template name (`SampleApp`, `SampleModule`, `SampleAction`, `SampleChange`, `SamplePlugin`).
- A request name cannot end with `Request`, `Response` or `Handler`. A command name cannot end with `Command`, `Result` or `Handler`.
- `add request -m` and `add command -m` use the module name as its folder has it, so `-m billing` gives `Billing`.
- The tool writes all files or no files. If one file exists, the tool stops and writes nothing.
- `add request` and `add command` stop if the module does not exist.
- `add module` stops if `Plugins/<Name>/` exists, and `add plugin` stops if `Features/<Name>/` exists: the two would have the same assembly names.
- `new app` writes a `nuget.config` that gets the VSA packages from GitHub Packages, with the token in `GITHUB_TOKEN` (see `docs/consumers.md`). Each `--source` replaces that feed, for example a local folder.
- `new app` writes a `.gitignore` for the build folders, also `bin~/` and `obj~/` of the layer projects in `Features/<Feature>/Projects/`.

## Template format A

```
framework/Tooling/Cli/Templates/
├─ AGENTS.md.template                 new app (the tool removes ".template")
├─ App/Host/, App/Views/              new app
├─ App/Views/Pages/SampleModulePage.razor          add module
├─ Features/SampleModule/**                         add module
│  ├─ Projects/SampleModule.<Layer>.csproj         add module (eight layer projects, with Domain.Shared)
│  ├─ Projects/SampleModule.*.Commands.csproj      add command (Contracts.Commands, Handlers.Commands)
│  ├─ Domain/Shared/SampleModuleKey.cs             add module (the published domain)
│  ├─ Slices/GetStatus/**                          add module (the status request)
│  ├─ Slices/SampleAction/**                       add request
│  └─ Slices/SampleChange/**                       add command
├─ Features/.gitkeep                                new app (keeps the empty Features/ folder)
├─ Plugins/SamplePlugin/**                         add plugin (root, Contracts/Commands/SamplePluginStep.cs)
├─ Plugins/.gitkeep                                new app (keeps the empty Plugins/ folder)
├─ Tests/App.Tests.csproj                         new app (the architecture tests come from the package)
├─ Tests/SampleModuleStatusTests.cs                add module
├─ Directory.Build.props, VSA.Sdk.Dev.props        source mode only, not copied
```

The path of a template file gives its command. The tool uses the first rule that matches:

| Rule | Path | Command | Replaced names |
|---|---|---|---|
| 1 | Contains `SampleAction` | `add request` | `SampleAction`, `SampleModule` |
| 2 | Contains `SampleChange`, or is `Projects/SampleModule.Contracts.Commands.csproj` or `Projects/SampleModule.Handlers.Commands.csproj` | `add command` | `SampleChange`, `SampleModule` |
| 3 | Contains `SamplePlugin` | `add plugin` | `SamplePlugin` |
| 4 | Under `Features/SampleModule/`, or file name contains `SampleModule` | `add module` | `SampleModule` |
| 5 | Other | `new app` | `SampleApp` |

`ScaffoldingTests.EachTemplateFileHasOneCommand` lists each template file with its command. If you add a template file, add it to this test.

The tool also replaces the kebab form of each name, for routes: `sample-module` becomes `billing`, and `sample-action` becomes `create-invoice`.

A new module has no command projects. The first `add command` of a module writes them, and the next commands use them. Thus a module with no command has no empty projects.

## Source mode and package mode

The templates build in this repository in source mode, as `samples/` does. The tool changes each `.csproj` to package mode:

| Source mode (repository) | Package mode (consumer) |
|---|---|
| `<Import ... VSA.Sdk.Dev.props />` and `<Import ... Sdk.targets />` | `<Project Sdk="VSA.Sdk" />` |
| `<ProjectReference Include="$(VsaFrameworkSourceDir).../VSA.Host.csproj" />` | `<PackageReference Include="VSA.Host" Version="<tool version>" />` |

If a template `.csproj` has a source-mode property that the tool cannot change, the tool stops with an error.

## Tests of a new app

`Tests/App.Tests.csproj` runs NUnit on Microsoft.Testing.Platform: `new app` writes the `test` runner in `global.json`. A new app has no central package versions, so the template sets the NUnit versions. They must be the same as in `Directory.Packages.props` (`ScaffoldingTests.TheTestTemplateUsesThePackageVersionsOfTheRepository`). The templates do not import `Directory.Packages.props`.

The build adds the architecture tests to `Tests/` (see [architecture-engine.md](architecture-engine.md)). The tests write no test class.

## One version

`new app` writes the version of the tool to these files. `vsa doctor` reads them and fails if two versions are different. `vsa update` writes one version to all of them.

| File | Item |
|---|---|
| The running tool | `VSA.Cli` |
| `dotnet-tools.json`, `.config/dotnet-tools.json` | `vsa.cli` |
| `global.json` | `msbuild-sdks` → `VSA.Sdk` |
| Each `.csproj` and `Directory.*.props` | Each `VSA.Sdk` pin (`Sdk="VSA.Sdk/1.0.0"`, `<Sdk Name="VSA.Sdk" Version="1.0.0" />`) and each `VSA.*` package reference. A reference with no version uses its `PackageVersion` |

## Update

`vsa update` moves an app to a new VSA version:

1. `dotnet tool update vsa.cli --local` changes the tool manifest. With no `--version`, it gets the latest version on the feeds of the app. If the app uses a preview, it also accepts previews. With `--version`, it gets that version, also a lower one.
2. The tool writes the version of the manifest to `global.json` and to each place in the table above. It changes no other item. A package reference with no version keeps its `PackageVersion`.
3. The tool shows the changed files. Then run `dotnet build`.

The running tool is the old version. So run `vsa doctor` after the update, not in the same command.

## Empty Domain

A new module has an empty Domain. When you add the first aggregate, write an `IRepository<T, TId>` for it in `Infrastructure/`, and register it in the module root.
