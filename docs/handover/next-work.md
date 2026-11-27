# Next work: the unified engine

This plan replaces brief D. It follows the decisions in `open-questions.md`, sections "Answers" and "Check system topology". The target is on the page "Check System Topology" (https://claude.ai/artifact/GQkEAJ48rdzvsDjPtXfBgf).

Base: `main`. Small PRs, one reviewer subagent for each diff. The user decides each merge of a PR about rules, analyzers or the public API.

## State on 2026-11-27 (short)

| Phase | State |
|---|---|
| 0 Spike | Done. The Visual Studio test waits for the computer of the user |
| 1 Runtime base | Done: `netstandard2.0`, `LocalHost` and `Send`, own TOML reader, no default interface methods, utility folder |
| 2 Packages | Done: `Umpire`, `Umpire.Vsa`, `Umpire.Definition`, `Umpire.Vsa.Definition`, `Umpire.Engine`; no SDK; only DependencyInjection as a third-party runtime DLL; lowest targets |
| Test speed | Done: `CS8032` fixed, two test sets, shared feed. Full set about 4 min, inner loop about 35 s plus the build |
| 3 Engine | Done: `EngineApp<THost>`, jobs and operations, engine library on `netstandard2.0`, the check model and all analyzers inside the engine |
| 4 Switch | Part 1 done (#406): the build checks on the model path from the snapshots of the compile, and no PDB is read for type facts. Part 2 done (#412): the engine reads reference folder DLLs into the model with `MetadataLoadContext`; ArchUnitNET, `TypeFactSource` and the Rules operation are gone |
| 5 Consumers of the engine | Done (#407): the `umpire` tool is an app on `ConsoleHost`, `umpire check` runs the engine of the app, `VsaUnitTest` sends requests through `LocalHost`. The definition test base keeps the engine queries |
| 6 Release | Not started: needs the computer of the user |

Answers of 2026-11-27 (see `open-questions.md`): reference folders C (read with `MetadataLoadContext`, ArchUnitNET goes), the tool B (a VSA app in `tooling/`), exit code C (the tool gives the exit code of its child).

Open questions from #412:
1. A project DLL in `deps.json` with no snapshot gives `UMP030802`, and the engine does not read it. A: keep it (now). B: read the DLL facts as a fallback, with a warning. Opinion: A.
2. Base type chains in Unity. A: vsa-unity adds the Unity DLL folder to `umpire.check.references`. B: the engine finds the Unity DLLs. Opinion: A.

Done (#413): the `umpire` tool is a VSA app in `tooling/` and a checked block; after a signal, `umpire test` and `umpire check` give the exit code of the child. Its Q2 (exit code 130 for a partial `umpire test`) is accepted as a technical choice.

Bug from #413: the check skipped the in-block types of each assembly `Umpire.*`, so the engine core was not checked. Fix with option A (an in-block type is never placed on an External by assembly name) in session `session_01Y7UMGY6CxBT2EeraWoDZbc`, branch `claude/place-in-block-types`.

Done (#417): the check reads the in-block types of each assembly `Umpire.*`; the engine core is checked. Utility split into `Paths` (Domain may use it) and `Utility` (Infrastructure only); the `BlockPlaces` folder walk is a fact source. Technical choices accepted: each `External` binds only types with their source outside the block; API breaks with no obsolete shims (before 0.1.0). Follow-up (option B, `BlockPlaceCheck` and other domain I/O, a folder reader for `Umpire.Testing`) in session `session_01Y7UMGWfAHqKDkn4RXHhgf1`, branch `claude/analyzer-entry-check`.

Watch: two full runs had 5 failures in `UmpireToolTests` while another session used the machine. The class passed alone and in the third full run.

The user reviews the "End review" list below at the end.

## Phases

| # | Phase | Depends on | Done when |
|---|---|---|---|
| 0 | Spike | None | Each assumption has a pass or fail. The user ran the Visual Studio test |
| 1 | Runtime base | 0 | The runtime targets `netstandard2.0`. `LocalHost` and `Send` work. The utility folder holds the shared code |
| 2 | Packages | 1 | Four packages: `Umpire`, `Umpire.Vsa`, `Umpire.Definition`, `Umpire.Vsa.Definition`. No SDK. Only DependencyInjection as a third-party DLL |
| 3 | Engine | 1 | `engine/` is `EngineApp<THost>` on `netstandard2.0`, with jobs, operations, and the plugins Jobs and Facts. `check/` and our analyzers are inside it |
| 4 | Switch | 2, 3 | The build uses the new engine. No ArchUnitNET. A PDB is read only for a DLL with no snapshot |
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
| Done: `EngineApp<THost>`, the build job and the check job, the operations, the plugins Operations and Facts (PR #401). Done: the engine library on `netstandard2.0`, `Entries/Console/`, `check/` and `architecture/Queries/` in the engine (PR #403, #404). Done: the analyzers and `Entries/Analyzer/` with the compile job; `Umpire.Engine.Analyzer.dll` is the only Umpire analyzer; `check/` is removed (PR #405) | P1, entry design |
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
| Done (PR #412): no ArchUnitNET, no `TypeFactSource` and no Rules operation. ExportModel reads the DLLs of a reference folder (`umpire.check.references`, Unity) with `DllSource` (`MetadataLoadContext`, IL and PDB), and CheckModel checks their types. `tests/Architecture.Tests` and the parity tests run on the model path. `SymbolReader` stays for the Blocks operation and for the DLL facts | K1, Q8, reference folders C |
| Done (PR #406): `block.json` stays. The compile needs the projects of the block, the place of each snapshot, the mode, the severities and the marks, and the definition does not hold them | P6 |
| Done (PR #406): brief D item 6 does not apply with no SDK. A multi-target root builds with no `<TargetFramework />`, and the package test has no workaround. Each inner build checks its own target | P7 |

## Phase 5: consumers of the engine

| Work | Decision |
|---|---|
| Done (PR #407): `Umpire.Testing`: `VsaUnitTest` starts the app of the test on a `LocalHost` and sends each request with `app.Send`. The definition test base keeps the queries of the engine: a job needs a built block, a bin and the definition loader of the console entry, and a definition test has only a type and a folder | P7, Q7 |
| Done (PR #407): the tool is an app, `UmpireTool : App<ToolKernel, ToolHost>`, and `ToolHost` is a `ConsoleHost`. The command words, the options and the messages stay the same | P7 |
| Done (PR #407): `umpire check` runs the check job with the `Umpire.Engine` of the app (`UmpireEngineFile` after a restore) | Q2 (watch) |

## End review

Uncertain points of this work. The user reviews them at the end of the plan, not during the work.

- Unify with the sibling design talk ("Design: host-agnostic app and entry points", draft spike #381, page https://claude.ai/artifact/F6jvJV9kdCe34Y2Jto2obD). Its "App and host split" (the app names only its kernel, the entry gives the host as a value, one fixed composition, an untyped `LocalHost.Invoke` in the core) differs from the "Entry design" of this plan (`EngineApp<THost>`). This plan continues with its own entry design until the unification pass.
- PR #407, the consumers of the engine:
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
- #405, the analyzers in the engine:
  - `Umpire` depends on `Umpire.Engine` with `PrivateAssets="none"`, so each project gets the analyzer entry and its DLLs. #398 had chosen `Umpire.Engine` only in the root project, so a library or a host that vendors the DLLs did not get the engine. Now each Umpire project downloads it.
  - The analyzer items leave out the `System.*` DLLs. On the .NET Framework compiler (Visual Studio, `msbuild.exe`), `System.Text.Json`, `System.Memory` and similar DLLs may be missing. Not tested: the Visual Studio steps are in PR #405.
  - The pack selection and the model facts are fact sources that the entry implements, with a `ConditionalWeakTable`: lazy, not plain data. `CompileJobReport` has two extra flags (`InModel`, `ChecksTypes`).
  - The engine self-check reads only the bin of the console entry, so it does not check the analyzer DLL.
  - `MetadataLoadContext` is pinned to 8.0.1 for `netstandard2.0`.
  - A restore target for the entry can race in a parallel build of a fresh clone.
  - The message of `UMP010003` still says "embeds Umpire.Config".
- #403 and #404, the engine core and the check model:
  - The queries keep the namespace `Umpire.Architecture`, and the model keeps `Umpire.CheckModel`, as an exception to the namespace rule (written in `AGENTS.md`).
  - `ArchitectureTest` needs the queries, so `Umpire.Vsa.Testing.Unit` and the `Umpire.Testing` bundle compile a copy of the query files: two copies of one source. `Umpire.Definition` gives its internals to `Umpire.Engine` and `Umpire.Testing`.
  - `AppLoader` stays in the console entry, because `AssemblyLoadContext` has no `netstandard2.0` API.
  - `engine/Polyfills~/` holds own copies of some .NET APIs for `netstandard2.0` (for example `FolderSearch`).
  - `umpire-check` is now `jobs check --model true`. Its exit code is the engine report code: 1 only for an error (before: 1 also for warnings, 2 for a broken check, 3 for bad flags). With `--model true`, the job still needs `--project` and `--bin`.
  - No job sends the export operation yet.
- #401, the engine as jobs and operations:
  - The plugin from `Steps` is named `Operations`, not `Jobs`: a feature `Jobs` and a plugin `Jobs` give two module classes with one name.
  - Each job has its own report type (`BuildJobReport`, `CheckJobReport`, subtypes of `JobReport`), because `UMP020302` needs the response in the slice of its handler.
  - The two job handlers repeat the same check sequence (about 50 lines), because `Slices/Shared/` cannot send the commands of other modules (`UMP020101`).
  - The console entry is `EngineApp<EngineConsoleHost>`: `ConsoleHost` gives no output for a result.
  - `--stage Build` (a build with no check) is removed. A call with no job word gives `UMP030401` with the fix; a new hook with an old engine still gives `UMP030004`. vsa-unity may call the engine with the old form.
  - New engine rule line: `rules.For<Host>().Uses<Contracts.Requests>().And<Domain.Shared>().Under<Plugin>()`, and the Host may use `Infrastructure` of a plugin (`CheckMode`).
  - `IFactSource<TQuery, TFacts>` has two type parameters. Some file reads stay outside Facts (`TargetRuntime`, `DefinitionFolder`, the Changes hashes, `EntryFiles`).
- #400, the lowest targets: `Umpire.Web` is `net9.0`, not the `net8.0` of P7, because `Views.Map` calls `MapStaticAssets` (ASP.NET Core 9); support for .NET 8 and 9 ended on 2026-11-10, so "lowest supported" may now mean `net10.0`. `Umpire.Console` stays `net8.0`: on .NET 10 on Linux, SIGTERM did not raise `ProcessExit`. `Umpire.Testing` keeps an ASP.NET Core framework reference only for `AddLogging`. `App.Services` is `protected internal` and is no longer in `IApp`.
- #399, `Configuration` and `Logging`: `HostArguments` keeps the old internal name. `WebHost` gives only the command-line arguments to `HostArguments`, not the full ASP.NET configuration. `OpenDescriptors` no longer closes open descriptors of `Microsoft.Extensions.*` assemblies. `Compose` now stops with an error if a service registers an `ILoggerProvider` and no `ILoggerFactory`. Parent keys such as `Probe` (of `Probe:Value`) are gone. Also: a race between test processes on the Notes fixture is fixed with `FixtureLock`.
- #398, the removal of the SDK:
  - A pack kernel with its own packages needs a manual `ProjectReference` in the root project; a package cannot add it to the restore.
  - The target `_UmpireRestoreAddedReferences` runs a restore inside the build, an unusual pattern for IDEs.
  - The source mode is selected by a regular expression on the text of the project file.
  - The root project references `Umpire.Engine` itself (else `UMP030003`); it is not a dependency of `Umpire`.
  - The engine state files did not move to the intermediate output path yet (phase 3).
  - `init` does nothing if `Directory.Build.props` exists. The fixtures `Built/NoPack` and `Built/PackGraph` seem unused.
- The tool as a VSA app (`tooling/`, options B and C):
  - Fixed later (the in-block types): the check bound each in-block type of an assembly `Umpire.*` to the External `UmpireLibraries` (`Places.Place`), so it did not check these types. The tool assembly was `umpire-cli` for this reason. It is `Umpire.Cli` again.
  - `UmpireTool<THost>` has an open host, so the tests run the tool in the test process on a `LocalHost`.
  - `ToolHost` wraps the operation of each request: the input type of the operation is `ToolInput`, and the request comes from `IToolRequest<TSelf>.From` when the operation runs. `ConsoleHost` got two extension points: `Select` and `CancelsOnSignal`.
  - The command line types (`CommandForm`, `CommandOption`, `CommandInput`, `ToolCall`) are in `Domain/Shared/` of the plugin, because `Contracts.Requests` holds only requests and responses. The host uses them by a rule of `ToolArchitecture`.
  - The handlers still throw `UmpireException`. `ToolHandler<,>` changes it into an `Error`.
  - After a signal, the tool sends SIGTERM to the child with the `kill` command (not on Windows), waits 10 seconds, then kills its process tree. On Linux a killed child gives exit code 137. If `umpire test` stops before its last project, it gives 130 when the last child gave 0. A `CancelsOnSignal` host whose operation throws the cancel still ends with exit code 0.
  - `CommandOption.Required` only changes the usage text. Each handler checks its required options (`form.Missing`), because the order is the name, then the app folder, then the options.
  - `Templates/` is a block of its own inside the tool block. Its files go into the tool as resources by a target (`_UmpireCliTemplates`), because `Umpire.After.targets` removes the files of a nested project.
- PR #406, the switch to the model path:
  - The analyzer of a build compile saves only and reports no type finding; the build job reports all. An editor compile checks types with the state of the last build (`state.txt`).
  - "Selected by hash" of brief D item 1 is read as: one snapshot file for each target (the intermediate path has the target), and the check selects the snapshot of each target.
  - A multi-target root checks in each inner build, so a finding of two targets is in the build output one time for each inner build. One check in the outer build needs `buildMultiTargeting/` files in each Umpire package.
  - With no `--state`, the engine takes the state folder from `--bin` (the folder `bin` becomes `obj`). The in-process tests use this.
  - A build with `-p:BuildProjectReferences=false` gives the snapshots of the direct references only.
  - Each compile in a build writes a snapshot, also of the framework projects and the test projects of this repository.
  - `state.txt` next to a snapshot names the state of the last root build that checked the project. Two roots that share a project write over it.
  - With no `--state`, no key and no `bin` folder in `--bin`, the state folder is `artifacts~/umpire` in the block (a `~` folder, so Unity does not import it).
- PR #412, the switch without ArchUnitNET:
  - ExportModel reads the facts of an app DLL only if the DLL has no snapshot and `deps.json` does not list it as a project. A project with no snapshot still gives `UMP030802`.
  - The DLL facts follow the IL. A `nameof`, a constant or an enum value is not a use, and a type of compiler output is a use. See "Compile facts and DLL facts" in `check-model.md`.
  - If a reference DLL does not resolve (for example `UnityEngine.dll` is not in a reference folder), a type keeps only the name of its direct base. A rule that names a deeper base type cannot see it. vsa-unity can add the folder of the Unity DLLs to `umpire.check.references`.
  - The hash of a file comes from the PDB, only for SHA-256. With another algorithm, the file is never old. Not tested with a Unity PDB.
  - An `External` DLL with a PDB and no library file gets library facts from the DLL, so its types can bind to a placed block body.
  - Changed expectations: 5 lines in `VsaAppTests` (the line of the use), the base-type exposures of `BellPresenter` (the model has no `System.Object` base exposure), the model part tree has no `(root)` part, the module list of the engine has no `Rules`. The two tests of the rule values (`umpire.<key>`, `umpire.<code>.<key>`) test the severity keys: no rule read another value.
  - `tests/Architecture.Tests` keeps its query tests on plain data, because the definition test base uses the queries.
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
- The analyzer entry passes the check (option B of PR #417): move the snapshot data to `Model/Domain/Shared`, put the rule list (`CodeRule`, `CodeNotice`, `RuleCodes`) behind a request, give `CompileHost` the module classes by a rule of `Entries` as for `Host`, and send the compile job through a published contract. Then remove the `Skip` of `BlockStampTests`.
- vsa-unity, before a Unity release: the pipeline closes each handler call at run time with `MakeGenericType` and `Activator.CreateInstance` (#382). A call with a struct type argument, for example `CommandHandlerCall<X, int>`, can fail on an IL2CPP player if no code for that type exists at build time. Test a player with an `ICommand<int>` command.
- vsa-unity, at the next consumer update: `Get<T>` of `UmpireConfig` binds module settings with reflection (#388). Under IL2CPP stripping, a removed setter is skipped with no error. A Unity host keeps the setters (`link.xml`, `[Preserve]`) or adds its own `ISettingsProvider`.

## Limits

- Do not change the VSA rule lines before K5.
- Measure only, do not change: the size of a snapshot (about 7 KB for each type).
- The in-block types of an assembly `Umpire.*` (`Places.Place`, `PartOf`):
  - An `External` binds only a type with its source outside the block folder, or in no folder alias. A type with its source in the block binds by its folder. This applies to each `External`, not only to `UmpireLibraries`: a consumer `External` for an assembly with its source in the block no longer binds these types. The External `UmpireLibraries` binds the Umpire DLLs of a consumer (package DLLs and the library files). The kernel bodies of `Umpire.dll` and `Umpire.Vsa.dll` still bind by their PDB path.
  - The placement does not count a folder whose name starts with `.` or ends with `~` (for example `engine/Polyfills~/`), as the source folder scan already does.
  - The engine core is now checked. The fixes: the rule types of the code check (`CodeRule`, `CodeRun` and the other helpers of `Slices/CheckCode/`) are internal, so they do not expose a published domain type in public; `CodeRuns` moved into that slice; `DefinitionExport`, `DefinitionSources` and `TraitExport` moved to `Model/Infrastructure/Export/`; `LoadAppHandler` calls `DefinitionFolder.IsSkippedSource`; `SharedAssemblies` names `Umpire.Kernels` by `LoadedTypes`.
  - Decided by the user: the pure helpers `RelativePath`, `BuildOutput` and `CompilerNames` (before `LoadedTypes.IsCompilerGenerated`) are in a new part `KernelParts.Paths` (`framework/Kernel/Paths/`). The engine definition lets `Domain` use only `Paths`, and `Infrastructure` uses `Paths` and `Utility`. The folder walk of the block places is in the fact source `BlockFolderSource` (Facts `Infrastructure/`): `ResolvedArchitecture.EmptyPlaces` takes its `BlockFolders` data (each folder of each place pattern, with `Exists`), and `BlockPlaceFolders()` gives the patterns for the query. No engine operation calls `EmptyPlaces`, so the source is not registered; the check model `BlockPlaceCheck` (`Model/Domain/Rules/`) still reads the folders itself. API breaks of the shipped DLLs: `LoadedTypes.IsCompilerGenerated` is now `CompilerNames.IsGenerated`, and `EmptyPlaces` of `Umpire.Testing` needs a `BlockFolders` argument, which a consumer test must build itself.
  - Open question: the analyzer entry (`engine/Entries/Analyzer`, assembly `Umpire.Engine.Analyzer`) gives about 70 `UMP020101` findings when a check reads its DLL. The build of `engine/` does not read it, because the bin of the console entry has no analyzer DLL. `BlockStampTests` skips it (`Skip`). The user accepted this (option C). The follow-up is option B (see "Later").
