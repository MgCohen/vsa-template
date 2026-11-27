# Next work: the unified engine

This plan replaces brief D. It follows the decisions in `open-questions.md`, sections "Answers" and "Check system topology". The target is on the page "Check System Topology" (https://claude.ai/artifact/GQkEAJ48rdzvsDjPtXfBgf).

Base: `main`. Small PRs, one reviewer subagent for each diff. The user decides each merge of a PR about rules, analyzers or the public API.

## Phases

| # | Phase | Depends on | Done when |
|---|---|---|---|
| 0 | Spike | None | Each assumption has a pass or fail. The user ran the Visual Studio test |
| 1 | Runtime base | 0 | The runtime targets `netstandard2.0`. `LocalHost` and `Send` work. The utility folder holds the shared code |
| 2 | Packages | 1 | Four packages: `Umpire`, `Umpire.Vsa`, `Umpire.Definition`, `Umpire.Vsa.Definition`. No SDK. Only DependencyInjection as a third-party DLL |
| 3 | Engine | 1 | `engine/` is `EngineApp<THost>` on `netstandard2.0`, with jobs, operations, and the plugins Jobs and Facts. `check/` and our analyzers are inside it |
| 4 | Switch | 2, 3 | The build uses the new engine. No ArchUnitNET, no PDB read |
| 5 | Consumers of the engine | 4 | `Umpire.Testing` and the tooling use the engine through their hosts |
| 6 | Release | 5 | A preview, then the consumer updates (local computer) |

## Phase 0: spike

The code is on the branch `claude/spike-localhost` (no PR).

| # | Assumption | Result |
|---|---|---|
| 1 | The runtime builds on `netstandard2.0` | Pass with fixes. Blocker: the default interface methods of `IIntegrationEventHandler` and `IPresenterHandler`. Small fixes: `Index`/`Range`, `ToHashSet`, `Path.GetRelativePath` and similar APIs |
| 2 | DependencyInjection loads inside the compiler | Pass. Each dependency DLL must be an analyzer item |
| 3 | `Send` through `LocalHost` works synchronously in a compiler callback | Pass. First composition 112 to 135 ms, one time for each compiler process. A later `Send` takes about 0.01 ms |
| 4 | The rule engine accepts an app class with an open host | Pass with a fix: `AppClass.KernelsOf` must refuse only an open kernel |
| 5 | The same works in Visual Studio | Skipped for now: the computer of the user is not available. The steps are in the spike report |

Also for phase 1: in the compiler, the module scan and the `.umpireconfig` search use the compiler folder. The analyzer entry needs a fixed module list and its own config path.

Follow-up: the default interface methods are removed in draft PR #382 (accepted design). Phase 1 takes it over.

## Phase 1: runtime base

| Work | Decision |
|---|---|
| Done: the test failures of `main` (#383, PR #384) | Clean base |
| Done: no default interface methods, one handler call for each kind (PR #382) | DIM decision |
| Done: the API sites that `netstandard2.0` does not have (PR #386) | P2 |
| Done: the portable layer and the bundles target `netstandard2.0` (PR #392) | P2, entry design |
| If an app class still has a generic parameter after the app and host split, fix `AppClass.KernelsOf`: refuse only an open kernel | Spike, assumption 4 |
| The analyzer entry gets a fixed module list and its own `.umpireconfig` path | Spike finding |
| Split the app, the composition and the host: `App` is the lifecycle with one start method and gets the host as a value, `Composition` only installs (core, kernel, modules, the self-install of the host), `AppHost` is only the transport | App and host split |
| One operation flow for all transports. Each transport only converts its input (arguments, a request, a payload) and its output. `IRequest` is a basic type | App and host split |
| Done: `LocalHost` and `Send`, through the `Operation` path, with a typed `Send` in the VSA kernel (PR #393). The engine accepts an app class with an open host | Entry design |
| Done: `LoadedTypes` and `ClosedBase` in `framework/Kernel/Utility/`, namespace `Umpire.Utility` (PR #387). Still to move: the file writer and the version check, with phase 2 | Q1, P8 |
| Done: our own TOML reader, no Tomlyn (PR #388) | K4 Q4 |
| Done: `App.Services` is `protected internal`, and `IApp` has no `Services`. The tests read it through `InternalsVisibleTo`. The consumer repos do not read it. A Unity bridge gets the provider in `UnityHost.Start(services, stop)` | Entry design |

## Phase 2: packages

| Work | Decision |
|---|---|
| Done: `Umpire.dll` ships no definition code. New packages `Umpire.Definition` and `Umpire.Vsa.Definition` (PR #391) | K4 Q1, Q2 |
| Done: the core `Umpire` and `Umpire.Vsa` are two packages and two DLLs (PR #394) | K4 Q6 |
| Done (PR #399): the core has no `Microsoft.Extensions.Configuration` and no `Logging`. It uses `HostArguments` and the null logger. Update the consumer repos: `Configuration(args)` is `Arguments(args)`, `Compose` takes `HostArguments`, and a non-web module reads `HostArguments` in place of `IConfiguration` | K4 Q5 |
| Done: no SDK. A consumer project uses `Microsoft.NET.Sdk` and package references; the build logic is in the `build/` files of `Umpire`; the engine ships in the new package `Umpire.Engine` (PR #398) | P7 |
| Done (PR #400): each package targets the lowest framework that its code needs. `Umpire.Web` is `net9.0` (`MapStaticAssets`), `Umpire.Console`, `Umpire.Testing` and `umpire` are `net8.0`. See "Runtime targets" in `requirements.md` | P7 |

## Phase 3: engine

| Work | Decision |
|---|---|
| `EngineApp<THost>` on `netstandard2.0`. `Entries/Console/` holds `Program.cs`, and `Entries/Analyzer/` holds the analyzer | P1, entry design |
| Jobs are requests in `App/Features/Jobs/`: compile job, build job, check job | P3, P9 |
| Operations are feature modules: export, setup check, architecture check, code check, format, tests | P3, P4 |
| Each job returns the same report type. The operations fill it | P4 |
| Plugins: Jobs (from `Steps`) and Facts (from `BuiltApp`, `IFactSource<T>`) | P5 |
| Move `check/` and `architecture/Queries/` into the engine | P1, P10 |
| Move the kernel analyzers and `UMP010201` into the code check operation | P1, Q6 |
| Done: `UMP010206` removed and its ID retired (PR #385) | Brief D item 5 |
| `nameof`, a constant and an enum value are a use | K3, brief D item 4 |

## Phase 4: switch

| Work | Decision |
|---|---|
| Done (PR #406): the build job sends ExportModel, then CheckModel. Each compile saves one snapshot for each project and target in its intermediate output path, with no definition. A finding that two targets give is in the report one time, and ExportModel deletes the snapshot of a removed target | P6, brief D items 1 and 2 |
| Done (PR #406): the package build runs the compile job on the source of each bundle (`UmpireLibraryPass`), and `Umpire.Engine` ships `tools/net8.0/libraries/<assembly>.json`. The check reads no PDB for the type facts | Q8 |
| Done (PR #406): the build runs the build job on the model path. The same findings and codes on the fixtures. Only the place of some findings changes (the line of the use, the line of the type name) | Queue item 3 of the hand-over |
| Done (PR #406): the state files are in the intermediate output path (`obj/.../umpire/`), also the run stamp. The block stamps stay in `artifacts~/umpire-checks/` of the block, because each app that uses the block reads them | P7 |
| Remove `TypeFactSource`, the Rules operation and ArchUnitNET. Open: `tests/Architecture.Tests` (about 500 cases) reads the bindings of the old check, and the DLLs of a reference folder (`umpire.check.references`, Unity) have no snapshot yet, so only the old check reads their types. Then convert the parity tests. `SymbolReader` stays for the Blocks operation (the check entries and the DLLs of a block) until the snapshots give these facts | K1, Q8 |
| Done (PR #406): `block.json` stays. The compile needs the projects of the block, the place of each snapshot, the mode, the severities and the marks, and the definition does not hold them | P6 |
| Done (PR #406): brief D item 6 does not apply with no SDK. A multi-target root builds with no `<TargetFramework />`, and the package test has no workaround. Each inner build checks its own target | P7 |

## Phase 5: consumers of the engine

| Work | Decision |
|---|---|
| Done (PR #PRNUM): `Umpire.Testing`: `VsaUnitTest` starts the app of the test on a `LocalHost` and sends each request with `app.Send`. The definition test base keeps the queries of the engine: a job needs a built block, a bin and the definition loader of the console entry, and a definition test has only a type and a folder | P7, Q7 |
| Done (PR #PRNUM): the tool is an app, `UmpireTool : App<ToolKernel, ToolHost>`, and `ToolHost` is a `ConsoleHost`. The command words, the options and the messages stay the same | P7 |
| Done (PR #PRNUM): `umpire check` runs the check job with the `Umpire.Engine` of the app (`UmpireEngineFile` after a restore) | Q2 (watch) |

## End review

Uncertain points of this work. The user reviews them at the end of the plan, not during the work.

- Unify with the sibling design talk ("Design: host-agnostic app and entry points", draft spike #381, page https://claude.ai/artifact/F6jvJV9kdCe34Y2Jto2obD). Its "App and host split" (the app names only its kernel, the entry gives the host as a value, one fixed composition, an untyped `LocalHost.Invoke` in the core) differs from the "Entry design" of this plan (`EngineApp<THost>`). This plan continues with its own entry design until the unification pass.
- PR #PRNUM, the consumers of the engine:
  - The tool is not a VSA app. `ToolKernel` is a core `Kernel` that gives one `Operation` for each command, and the build does not check the tool as a block. A VSA tool (requests, handlers, modules, a checked block in `tooling/`, P10) is a larger move.
  - The tool operations read the command line from the service `ToolLine`, not from `HostArguments`. `ConsoleHost` only selects the operation by its words. The binding of `ConsoleHost` has no positional name, no short option and no option with many values.
  - On Ctrl+C or SIGTERM, `ConsoleHost` requests the stop with exit code 0, and the first request wins. So `umpire test` and `umpire check` can give exit code 0 after a signal, also if the child process fails. The engine console has the same behavior.
  - `umpire check` gives the folder of the root project as `--project`, as the build hook does. `dotnet build <folder>` stops with MSB1011 if the folder also has a solution file with another name.
  - `umpire check -c` takes only `Debug` and `Release`, because the engine finds the configuration in the name of the output folder (`BuildConfiguration`).
  - `VsaUnitTest` composes the full app: the default `ScannedModules` reads each DLL of the test output for each test, and each module of the test output must compose. A request handler must be in a module. A test class can give its own `Modules`. The test app is `UnitTestApp<TKernel>`, with an open kernel, so the engine does not count it as a check entry of `Umpire.Testing` (else `UMP020401` on `packs/Vsa/Tests/Unit`).
  - A test service replaces each app service of the same service type, and an open generic test service replaces each closed form. A request runs in the scope of `LocalHost`, so a test reads only singletons after `Handle`.
- #387: two rule lines follow the move of the type helpers: the part `KernelParts.Utility` in `Core`, and `For<Infrastructure>().Uses<KernelParts.Utility>()` in the engine definition.
- #388: a reader error now comes for two keys that differ only in case, and for TOML forms that the reader does not support.
- #391, the definition packages:
  - In package mode, the check gets the pack definition from the engine folder of the SDK package (`tools/net8.0/`), not from a NuGet restore. This keeps the definition DLLs out of the `deps.json` of the app. The other choice is a `PackageReference` with no assets.
  - Definition code in an app project (not a definition project) now fails to compile with CS0246. Before, the engine gave a guided finding (`UMP030511` or `UMP030308`). Is a guided finding necessary again?
  - `Testing/` (`ArchitectureTest`) stays in `Umpire.Definition`. The markers stay in `Umpire`.
  - The archive has a separate part, `definition/netstandard2.1/`.
  - The example check entry of the engine (`BlockSearch.cs`) and `docs/architecture-engine.md` name the assembly `Umpire.Vsa`, which is correct only in source mode. This is older than #391.
- #394, the split of the core and `Umpire.Vsa`:
  - Requirement W12 in `docs/requirements.md` now says "the core `Umpire.dll`, one DLL for each pack". It follows the K4 Q6 answer, but the subagent wrote the words.
  - The views and the VSA test bases stay in `Umpire.Web` and `Umpire.Testing`, so a web app with another pack still gets `Umpire.Vsa.dll`. A VSA web package is a later change.
  - The bundle project file is `tools/bundle/Vsa/Umpire.Vsa.Kernel.csproj`, because the source definition project is also named `Umpire.Vsa`. In source mode the assembly `Umpire.Vsa` is the VSA definition; in package mode it is the VSA kernel.
  - The core package removes the second copy of the kernel analyzer with a target (`build/Umpire.targets`). A second pack with its own kernel copy needs a check.
  - `Umpire.dll` gives its internals to `Umpire.Vsa` (`Module.Order`, `Module.InstallWith`, `Pipelines.SendAsync`, `Pipelines.PublishAsync`, `EnvelopeResults`). The list may not be complete.
- #400, the lowest targets: `Umpire.Web` is `net9.0`, not the `net8.0` of P7, because `Views.Map` calls `MapStaticAssets` (ASP.NET Core 9); support for .NET 8 and 9 ended on 2026-11-10, so "lowest supported" may now mean `net10.0`. `Umpire.Console` stays `net8.0`: on .NET 10 on Linux, SIGTERM did not raise `ProcessExit`. `Umpire.Testing` keeps an ASP.NET Core framework reference only for `AddLogging`. `App.Services` is `protected internal` and is no longer in `IApp`.
- #399, `Configuration` and `Logging`: `HostArguments` keeps the old internal name. `WebHost` gives only the command-line arguments to `HostArguments`, not the full ASP.NET configuration. `OpenDescriptors` no longer closes open descriptors of `Microsoft.Extensions.*` assemblies. `Compose` now stops with an error if a service registers an `ILoggerProvider` and no `ILoggerFactory`. Parent keys such as `Probe` (of `Probe:Value`) are gone. Also: a race between test processes on the Notes fixture is fixed with `FixtureLock`.
- #398, the removal of the SDK:
  - A pack kernel with its own packages needs a manual `ProjectReference` in the root project; a package cannot add it to the restore.
  - The target `_UmpireRestoreAddedReferences` runs a restore inside the build, an unusual pattern for IDEs.
  - The source mode is selected by a regular expression on the text of the project file.
  - The root project references `Umpire.Engine` itself (else `UMP030003`); it is not a dependency of `Umpire`.
  - The engine state files did not move to the intermediate output path yet (phase 3).
  - `init` does nothing if `Directory.Build.props` exists. The fixtures `Built/NoPack` and `Built/PackGraph` seem unused.
- PR #406, the switch to the model path:
  - The analyzer of a build compile saves only and reports no type finding; the build job reports all. An editor compile checks types with the state of the last build (`state.txt`).
  - "Selected by hash" of brief D item 1 is read as: one snapshot file for each target (the intermediate path has the target), and the check selects the snapshot of each target.
  - A multi-target root checks in each inner build, so a finding of two targets is in the build output one time for each inner build. One check in the outer build needs `buildMultiTargeting/` files in each Umpire package.
  - With no `--state`, the engine takes the state folder from `--bin` (the folder `bin` becomes `obj`). The in-process tests use this.
  - A DLL of a reference folder (`umpire.check.references`, for example the Unity `ScriptAssemblies`) has no snapshot, so the build job gives `UMP030802` for it and does not check its types. The Unity compile must save the snapshots first.
  - A build with `-p:BuildProjectReferences=false` gives the snapshots of the direct references only.
  - Each compile in a build writes a snapshot, also of the framework projects and the test projects of this repository.
  - `state.txt` next to a snapshot names the state of the last root build that checked the project. Two roots that share a project write over it.
  - With no `--state`, no key and no `bin` folder in `--bin`, the state folder is `artifacts~/umpire` in the block (a `~` folder, so Unity does not import it).
- #396, the shared feed: the hash includes each root file, so an edit of `AGENTS.md` or `README.md` makes a new pack. A feed with no use for two hours is deleted. The in-process tests of `UMP030512` and `UMP030513` delete a DLL from a copy of the bin, where the old cases used a real SDK build. The lock was not tested on Windows.
- #393, `LocalHost`:
  - `LocalHost` is not sealed, and `Send` exists for each host that derives from `LocalHost`, so a custom or test host also gets it.
  - The typed `Send` (VSA) and the untyped `Send` (core) have the same name. The caller gets the typed one only when it imports both namespaces.
  - The sync `Send` blocks on the task, so it can deadlock on a thread with a `SynchronizationContext` (for example the Unity main thread). The docs say to use `SendAsync` there. The sibling spike found the same point.
  - The operation lookup walks base classes only, not interfaces.
- #392, the move to `netstandard2.0`:
  - The archive folders are now `runtime/netstandard2.0/` and `definition/netstandard2.0/`. `docs/consumers.md` said that these names stay the same. vsa-unity must change its paths, and it may need to skip more DLLs that Unity already ships (`System.Memory` and others).
  - The SDK now gives `IsExternalInit` to a consumer `netstandard2.0` project too, with the opt-out `UmpireIsExternalInitPolyfill=false`.
  - The archive test compiles a `netstandard2.1` app against the `netstandard2.0` DLLs, as a model of Unity.
  - Not tested: the .NET Framework compiler of Visual Studio, and the archive tests that need a .NET 8 SDK.

## Test speed

- Done: `CS8032` fixed, the shared compiler is on again, the categories `Package` and `Parity`, and the two test sets in `AGENTS.md` (PR #395). A full run passes 1963 tests, with no `CS8032`.
- Done: one feed for each content hash, 13 workspaces in place of 42, and the merge of duplicate build tests (PR #396). The full set takes about 3.7 min, and the inner loop about 34 s plus the build.
- Next: CheckModel `Parity` (about 2:40) is now the longest project after Rules.Tests.

## Later

- K5 (#344), with P11 (the scope of a rule).
- The typed channel of `LocalHost`.
- Own checks of an app (option C, maybe B).
- #380: a handler returns `Task<T>`.
- A check at start that each handler has a call, and a call that names its own handler base (#382 review).
- The Visual Studio test of the spike (needs the computer of the user).
- A full cleanup and review of the tests, at the end of this phase. It includes the older path bugs that the #386 review found (a file on another drive counts as in the block, case in `InInnerBlock` and `RelativePath`, a folder name that starts with `..`, `OpenGenerics.NameOf` for a type nested in a generic type).
- vsa-unity, before a Unity release: the pipeline closes each handler call at run time with `MakeGenericType` and `Activator.CreateInstance` (#382). A call with a struct type argument, for example `CommandHandlerCall<X, int>`, can fail on an IL2CPP player if no code for that type exists at build time. Test a player with an `ICommand<int>` command.
- vsa-unity, at the next consumer update: `Get<T>` of `UmpireConfig` binds module settings with reflection (#388). Under IL2CPP stripping, a removed setter is skipped with no error. A Unity host keeps the setters (`link.xml`, `[Preserve]`) or adds its own `ISettingsProvider`.

## Limits

- Do not change the VSA rule lines before K5.
- Measure only, do not change: the size of a snapshot (about 7 KB for each type).
