# Scaffolding: the umpire tool

The `umpire` tool (package `Umpire.Cli`) writes new apps, modules, plugin modules, requests and commands from templates. The templates are one compilable app in this repository, so `Umpire.slnx` builds them as normal code (`dotnet run solution.cs` makes `Umpire.slnx`). The tool, Umpire.Sdk and the Umpire.* packages have one version. The rule engine checks it on each build (`UMP030201`), `umpire doctor` runs the same check, and `umpire update` moves all to the latest version.

To make a first app and fix a first rule error, see [getting-started.md](getting-started.md).

## Commands

| Command | Writes | Edits |
|---|---|---|
| `new app <Name>` | The root project `<Name>.csproj`, `App/Host` (`Program.cs`, `<Name>App.cs`), `App/Views`, `Tests/`, `Features/`, `Plugins/`, then the setup files of `init` | None |
| `init [--source <feed>] [--check true]` | Each missing setup file | Each setup file that has no Umpire setup: it adds the missing entry, element or line. See "Setup tasks" |
| `add module <Name>` | Module root, published domain, status request, presenter, page, unit test | None |
| `add request <Name> -m <Module>` | Request, response, handler | None |
| `add command <Name> -m <Module>` | Command, result, handler | None |
| `add plugin <Name>` | Plugin root (inherits `Plugin`), `Contracts/Commands/` with the base command `<Name>Step<TResult>` | None |
| `add pack <Name>` | Pack class `<Name><THost>` in `Packs/<Name>/<Name>Pack.cs`, and `Architecture/<Name>Architecture.cs` (inherits `VsaArchitecture`). See `pack-variants.md` | None |
| `doctor` | None | None |
| `update [--version <version>]` | None | The tool manifest, with `dotnet tool update`. Then the Umpire versions in `global.json` and the project files |

Rules for all commands:

- A name is a PascalCase C# identifier. Names that start with `VSA` are forbidden. These names are also forbidden, because they break the build: `App`, `Views`, `Pages`, `Home`, `Features`, `Plugins`, `Pack`, `Packs`, `System`, `Microsoft`.
- A name cannot contain a template name (`SampleApp`, `SampleModule`, `SampleAction`, `SampleChange`, `SamplePlugin`, `SamplePack`).
- A request name cannot end with `Request`, `Response` or `Handler`. A command name cannot end with `Command`, `Result` or `Handler`.
- `add request -m` and `add command -m` use the module name as its folder has it, so `-m billing` gives `Billing`.
- An option that takes one value stops the command if you give it two times, for example `-m A -m B`.
- Each command writes only new files, except `init` and `update`. `init` adds the missing setup to existing files, and `update` changes only the Umpire versions.
- Each command writes all files or no files, except `init` and `update`. If one file exists, the command stops and writes nothing. `new app` also stops if a setup file of `init` exists in the new folder.
- `add request` and `add command` stop if the module does not exist.
- `add module` stops if `Plugins/<Name>/` exists, and `add plugin` stops if `Features/<Name>/` exists: the two would have the same module name.
- `add pack` stops if `Packs/<Name>/` exists.
- `new app` writes a `nuget.config` that gets the Umpire packages from GitHub Packages, with the token in `GITHUB_TOKEN` (see `docs/consumers.md`). Each `--source` replaces that feed, for example a local folder.
- `new app` writes a `Directory.Build.props` that puts all build output in `artifacts~/` in the block folder (`UseArtifactsOutput`, `ArtifactsPath`). No `bin/` or `obj/` folder is next to the code, and Unity does not import the DLLs a second time: it skips each folder whose name ends with `~`. The .NET SDK reads this setting only from `Directory.Build.props` or the command line (`NETSDK1199`), so the setting is not in Umpire.Sdk.
- `new app` writes a `.gitignore` for the build folders and the local files (`artifacts~/`, `bin/`, `obj/`, `TestResults/`, `*.user`, `.vs/`).
- The app has one root project. A module, a request, a command and a plugin add only source files, and the root project compiles them.

## Add a command

Each command of the tool is one class in `tools/cli/Commands/`. It inherits one base from `Commands/Bases/`, and it has one entry in `CommandList.All`. The help text comes from that list.

```
ToolCommand                      Words, Summary, TakesName, Options, Usage, Run(CommandInput)
├─ NewAppCommand                 umpire new app <Name> [--source <feed>]
├─ InitCommand                   umpire init [--source <feed>] [--check true]
└─ AppCommand                    finds the block folder, then Run(root, CommandInput)
   ├─ DoctorCommand              umpire doctor
   ├─ UpdateCommand              umpire update [--version <version>]
   └─ AddCommand                 checks the name, writes the template files of one TemplateGroup
      ├─ AddModuleCommand        umpire add module <Name>
      ├─ AddPluginCommand        umpire add plugin <Name>
      ├─ AddPackCommand          umpire add pack <Name>
      └─ AddSliceCommand         one class, one instance for each SliceKind:
                                   SliceKind.Request  umpire add request <Name> -m <Module>
                                   SliceKind.Command  umpire add command <Name> -m <Module>
```

| You want | Inherit | You write |
|---|---|---|
| A command outside an app | `ToolCommand` | `Words`, `Summary`, `Run` |
| A command in an app | `AppCommand` | `Words`, `Summary`, `Run(root, input)` |
| A command that writes template files | `AddCommand` | `Noun`, `Group`, `TemplateNames`, `Heading`. Optional: `ForbiddenSuffixes`, `CheckFree`, `NextStep` |
| A slice kind with the shape of a request or a command | No class | A new `SliceKind` value in `SliceKind.All` |

To add a command:

1. Write the class in `Commands/`. Inherit the base from the table.
2. Give its options as `CommandOption` values. `Required` makes the option necessary. `Many` accepts more than one value.
3. Read the input with `input.Name`, `input.Single(option)` or `input.All(option)`.
4. For an error for the user, throw `UmpireException`. The tool shows the message and stops with exit code 1.
5. Add the command to `CommandList.All`.
6. Add a test in `tests/Cli`. `CommandTests` makes sure that each command inherits one base and is in the list.

An `AppCommand` checks the input in this order: the name, then the block folder, then the required options. `AddCommand.Run` makes the template names in one place, from `TemplateNames` of the command.

## Template format A

```
tools/cli/Templates/
├─ AGENTS.md.template                 new app (the tool removes ".template")
├─ SampleApp.csproj                   new app (the root project, becomes <Name>.csproj)
├─ SampleApp.slnx.template           new app (becomes <Name>.slnx)
├─ global.json.template              setup (the .NET SDK and the test runner)
├─ .umpireconfig                     setup (only for an app with more than one project)
├─ nuget.config.template             setup (each --source replaces the umpire feed and removes the credentials)
├─ .gitignore.template               setup
├─ appsettings.json, Properties/       new app
├─ App/Host/, App/Views/              new app (App/Host/SampleAppApp.cs becomes <Name>App.cs)
├─ App/Views/Pages/SampleModulePage.razor          add module
├─ Features/SampleModule/**                         add module
│  ├─ Domain/Shared/SampleModuleKey.cs             add module (the published domain)
│  ├─ Slices/GetStatus/**                          add module (the status request)
│  ├─ Slices/SampleAction/**                       add request
│  └─ Slices/SampleChange/**                       add command
├─ Features/.gitkeep                                new app (keeps the empty Features/ folder)
├─ Packs/SamplePack/**                             add pack (SamplePackPack.cs, Architecture/SamplePackArchitecture.cs)
├─ Plugins/SamplePlugin/**                         add plugin (root, Contracts/Commands/SamplePluginStep.cs)
├─ Plugins/.gitkeep                                new app (keeps the empty Plugins/ folder)
├─ Tests/App.Tests.csproj                         new app
├─ Tests/SampleModuleStatusTests.cs                add module
├─ Directory.Build.props, Umpire.Sdk.Dev.props        source mode only, not copied
```

The path of a template file gives its command. The tool uses the first rule that matches:

| Rule | Path | Command | Replaced names |
|---|---|---|---|
| 1 | Contains `SampleAction` | `add request` | `SampleAction`, `SampleModule` |
| 2 | Contains `SampleChange` | `add command` | `SampleChange`, `SampleModule` |
| 3 | Contains `SamplePlugin` | `add plugin` | `SamplePlugin` |
| 4 | Contains `SamplePack` | `add pack` | `SamplePack` |
| 5 | Under `Features/SampleModule/`, or file name contains `SampleModule` | `add module` | `SampleModule` |
| 6 | One of the four setup templates (`SetupTemplates` in `Templates.cs`) | The setup pipeline (`init`, `new app`) | None |
| 7 | Other | `new app` | `SampleApp` |

`ScaffoldingTests.EachTemplateFileHasOneCommand` lists each template file with its command. If you add a template file, add it to this test.

The tool also replaces the kebab form of each name, for routes: `sample-module` becomes `billing`, and `sample-action` becomes `create-invoice`.

The suffix `.template` keeps a file inactive in this repository, for example a `global.json` that would change the SDK of the template build. The tool removes the suffix. In each template file, the tool replaces `{{UmpireVersion}}` with its own version.

## Source mode and package mode

The templates build in this repository in source mode, as the fixtures do. The tool changes each `.csproj` to package mode:

| Source mode (repository) | Package mode (consumer) |
|---|---|
| `<Project>` with `<Import ... Umpire.Sdk.Dev.props />` and `<Import ... Sdk.targets />` | `<Project Sdk="Umpire.Sdk/<tool version>">` (one line if it has nothing else) |
| `<Project Sdk="Microsoft.NET.Sdk.Web">` with the same imports | `<Project Sdk="Umpire.Sdk.Web/<tool version>">` |
| `<ProjectReference Include="$(UmpireFrameworkSourceDir).../Umpire.Vsa.Hosts.Web.csproj" />` | `<PackageReference Include="Umpire.Vsa.Hosts.Web" Version="<tool version>" />` |

If a template `.csproj` has a source-mode property that the tool cannot change, the tool stops with an error.

## Tests of a new app

`Tests/App.Tests.csproj` is `<Project Sdk="Umpire.Sdk/<tool version>">` with one reference to the root project. Umpire.Sdk gives a project whose name ends with `.Tests` NUnit on Microsoft.Testing.Platform, and `new app` writes the `test` runner in `global.json`. The NUnit versions come from `Directory.Packages.props` of this repository, in the package and in source mode (`Umpire.Sdk.Dev.props`). The templates do not import `Directory.Packages.props`.

The rule engine checks the architecture after each build of the host (see [architecture-engine.md](architecture-engine.md)). Umpire.Sdk adds the build package `Umpire.Engine.Build` to the root project (an executable that is not a test project), so no project has this line. The engine reads the rules of `App/Host/<Name>App.cs`. The test project does not check the architecture. `-p:UmpireCheck=false` turns the check off for one build. `-p:UmpireCheckMode=Warn` reports each error as a warning.

## Setup tasks

`umpire init` adds the Umpire setup to an existing app, in the current folder. `umpire new app` writes its app files, then runs the same setup tasks. The tasks are the list `SetupTasks.Default` in `tools/cli/Scaffolding/Setup/`, with one class for each task in `Tasks/`.

```
umpire init                          umpire new app <Name>
   │                                     ├─ the app files (root project, App/, Tests/, slnx, AGENTS.md)
   ▼                                     ▼
SetupTasks.Plan ──► each task: Done | Change(file, content) | ByHand(message)
   ├─ show each task, with the message of each by-hand task
   ├─ --check true: write nothing, exit 1 if a task is not done
   ├─ write each change ("created" or "changed"), then list the by-hand tasks
   └─ exit 1 if a by-hand task is left
```

| Task | File | Done when | No file | A file without the setup | By hand |
|---|---|---|---|---|---|
| `global-json` | `global.json` | The file exists with no `msbuild-sdks` → `Umpire.Sdk` | The template | Nothing | A pin of `Umpire.Sdk` (the version is on the project line), comments |
| `nuget-config` | `nuget.config` | A source has the key `umpire`. With `--source`: each source path is a source | The template | Adds the `umpire` source and its credentials. With `--source`: each missing source, with a free key | Not valid XML |
| `git-ignore` | `.gitignore` | Each line of the template is in the file | The template | Adds the missing lines | None |
| `umpire-sdk` | The root project | The main SDK is `Umpire.Sdk` or `Umpire.Sdk.Web` with the tool version | Not possible | `Microsoft.NET.Sdk` becomes `Umpire.Sdk/<version>`, `Microsoft.NET.Sdk.Web` becomes `Umpire.Sdk.Web/<version>`, an Umpire SDK with no version gets it | Another version (`umpire update`), an `<Sdk>` element, another main SDK, not valid XML |
| `umpire-config` | `.umpireconfig` | The file exists, or the app has one project | Writes the template if the app has more than one project | Nothing | None |

Rules of a task:

1. A task only plans. The tool writes all changes after all tasks.
2. A task adds. It does not remove, reorder or change a value of the app.
3. A conflict is a by-hand task, with the fix in its message.
4. A changed file keeps its byte order mark and the line ending of each line. A new line gets the most frequent line ending of the file.
5. A new XML element gets the indent of the first child element of the file.
6. A changed JSON file keeps its indent, but the tool writes all of the JSON again in the indented form. So a JSON file with comments is a by-hand task.
7. Two tasks can change one file. A task plans on the planned content of the tasks before it.
8. A second run of `init` gives "done" for each task.

`init` needs exactly one `.csproj` in the current folder: the root project. It writes only in that folder. A file in a parent folder stays, and the new file does not hide it:

- The new `nuget.config` has no `<clear />`.
- The new `global.json` keeps the `sdk`, `msbuild-sdks` (with no `Umpire.Sdk`) and `test` sections of the parent one. It adds no `test` section of its own.

`new app` does not read the parent folders. `init` does not write the app class: if the app has none, the rule engine gives `UMP030302`.

To add a task:

1. Write a class in `Scaffolding/Setup/Tasks/` that inherits `SetupTask`. Give it an `Id` and `Plan(SetupContext)`.
2. Read each file with `context.Read(path)`. It gives the planned content, or the file on disk.
3. Return `Done()`, `Change(file, content)` or `ByHand(message)`.
4. Add the task to `SetupTasks.Default`, in its order.
5. Add tests to `tests/Cli/SetupTests.cs`. `ASecondRunOfEachTaskChangesNothing` runs each task two times.

The task ids are stable. A later version can let a consumer skip or add a task by its id.

## One version

`new app` writes the version of the tool to these files. The `doctor` command runs the version check of the rule engine (`VersionCheck`, see "One version" in [architecture-engine.md](architecture-engine.md)) with the version of the running tool. It fails if a slot has a different version. The `update` command writes the version of the tool (or `--version`) to all of them, and checks nothing.

| File | Item |
|---|---|
| The running tool | `Umpire.Cli` |
| Each `.csproj` and `Directory.*.props` | Each `Umpire.Sdk` and `Umpire.Sdk.Web` pin (`Sdk="Umpire.Sdk.Web/1.0.0"`, `<Sdk Name="Umpire.Sdk" Version="1.0.0" />`) and each `Umpire.*` package reference, also `Umpire.Engine.Build`. A reference with no version uses its `PackageVersion` |

The tool runs with `dotnet tool exec Umpire.Cli@<version> -y -- <command>`, so the app has no tool manifest. The commands that need an app (`add`, `doctor`, `update`) find the app folder: the nearest `.umpireconfig` at or above the current folder, else the nearest folder with a project that names an Umpire SDK and its version.

## Update

`umpire update` moves an app to a new Umpire version:

1. `dotnet tool update umpire.cli --local` changes the tool manifest. With no `--version`, it gets the latest version on the feeds of the app. If the app uses a preview, it also accepts previews. With `--version`, it gets that version, also a lower one.
2. The tool writes the version of the manifest to `global.json` and to each place in the table above. It changes no other item. A package reference with no version keeps its `PackageVersion`.
3. The tool shows the changed files. Then run `dotnet build`.

The running tool is the old version. So run `umpire doctor` after the update, not in the same command.

## Empty Domain

A new module has an empty Domain. When you add the first entity, write an `IRepository<T, TId>` for it in `Infrastructure/`, and register it in the module root.
