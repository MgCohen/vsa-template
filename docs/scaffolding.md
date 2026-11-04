# Scaffolding: the vsa tool

The `vsa` tool (package `VSA.Cli`) writes new apps, modules and requests from templates. The templates are one compilable app in this repository, so `VSA.slnx` builds them as normal code (`dotnet run solution.cs` makes `VSA.slnx`). The tool, VSA.Sdk and the VSA.* packages have one version, and `vsa doctor` finds a mismatch.

## Commands

| Command | Writes | Edits |
|---|---|---|
| `new app <Name>` | Root files, `App/Host`, `App/Views`, `Modules/` | None |
| `add module <Name>` | All layers, status request, presenter, page | Solution, with `dotnet sln add` |
| `add request <Name> -m <Module>` | Request, response, handler | None |
| `doctor` | None | None |

Rules for all commands:

- A name is a PascalCase C# identifier. Names that start with `VSA` are forbidden. These names are also forbidden, because they break the build: `App`, `Views`, `Pages`, `Home`, `Modules`, `System`, `Microsoft`.
- A name cannot contain a template name (`SampleApp`, `SampleModule`, `SampleAction`).
- `add request -m` uses the module name as its folder has it, so `-m billing` gives `Billing`.
- The tool writes all files or no files. If one file exists, the tool stops and writes nothing.
- `add request` stops if the module does not exist.
- `new app` writes a `nuget.config` that gets the VSA packages from GitHub Packages, with the token in `GITHUB_TOKEN` (see `docs/consumers.md`). Each `--source` replaces that feed, for example a local folder.

## Template format A

```
framework/Tooling/Cli/Templates/
├─ AGENTS.md.template                 new app (the tool removes ".template")
├─ App/Host/, App/Views/              new app
├─ App/Views/Pages/SampleModulePage.razor          add module
├─ Modules/SampleModule/**                         add module
│  ├─ Contracts/Requests/SampleAction/**           add request
│  └─ Handlers/Requests/SampleActionHandler.cs     add request
├─ Modules/.gitkeep                                new app (keeps the empty Modules/ folder)
├─ Directory.Build.props, VSA.Sdk.Dev.props        source mode only, not copied
```

The path of a template file gives its command. The tool uses the first rule that matches:

| Rule | Path | Command | Replaced names |
|---|---|---|---|
| 1 | Contains `SampleAction` | `add request` | `SampleAction`, `SampleModule` |
| 2 | Under `Modules/SampleModule/`, or file name contains `SampleModule` | `add module` | `SampleModule` |
| 3 | Other | `new app` | `SampleApp` |

`ScaffoldingTests.EachTemplateFileHasOneCommand` lists each template file with its command. If you add a template file, add it to this test.

The tool also replaces the kebab form of each name, for routes: `sample-module` becomes `billing`, and `sample-action` becomes `create-invoice`.

## Source mode and package mode

The templates build in this repository in source mode, as `samples/` does. The tool changes each `.csproj` to package mode:

| Source mode (repository) | Package mode (consumer) |
|---|---|
| `<Import ... VSA.Sdk.Dev.props />` and `<Import ... Sdk.targets />` | `<Project Sdk="VSA.Sdk" />` |
| `<ProjectReference Include="$(VsaFrameworkSourceDir).../VSA.Host.csproj" />` | `<PackageReference Include="VSA.Host" Version="<tool version>" />` |

If a template `.csproj` has a source-mode property that the tool cannot change, the tool stops with an error.

## One version

`new app` writes the version of the tool to these files. `vsa doctor` reads them and fails if two versions are different.

| File | Item |
|---|---|
| The running tool | `VSA.Cli` |
| `dotnet-tools.json`, `.config/dotnet-tools.json` | `vsa.cli` |
| `global.json` | `msbuild-sdks` → `VSA.Sdk` |
| Each `.csproj` and `Directory.*.props` | Each `VSA.Sdk` pin (`Sdk="VSA.Sdk/1.0.0"`, `<Sdk Name="VSA.Sdk" Version="1.0.0" />`) and each `VSA.*` package reference. A reference with no version uses its `PackageVersion` |

## Empty Domain

A new module has an empty Domain. The DbContext generator still writes `<Module>DbContext` with no aggregates. The starter module does not call `AddDatabase`, so the app does not use this context. When you add the first aggregate, call `module.AddDatabase(...)` in the module root.
