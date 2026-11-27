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
| Make `App.Services` internal or protected, after a check of its users | Entry design |

## Phase 2: packages

| Work | Decision |
|---|---|
| Done: `Umpire.dll` ships no definition code. New packages `Umpire.Definition` and `Umpire.Vsa.Definition` (PR #391) | K4 Q1, Q2 |
| Done: the core `Umpire` and `Umpire.Vsa` are two packages and two DLLs (PR #394) | K4 Q6 |
| Remove `Microsoft.Extensions.Configuration` and `Logging`. First list what `IConfiguration` loses in the consumer repos | K4 Q5 |
| Remove the SDK: the packages bring their own `build/` props and targets. This repository keeps `UseArtifactsOutput` | P7 |
| Each package targets the lowest framework that its code needs. `Umpire.Web` is `net8.0` | P7 |

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
| The export operation writes `definition.json` and the snapshots, one for each target framework | P6, brief D items 1 and 2 |
| The Umpire build saves `libraries/Umpire.json`, and the package ships it. No PDB read | Q8 |
| The build runs the build job of the new engine | Queue item 3 of the hand-over |
| Remove `BinReader`, `SymbolReader` and ArchUnitNET | K1, Q8 |
| Examine whether `block.json` is necessary | P6 |
| Check whether brief D item 6 (a multi-target root clears `TargetFramework`) still applies with no SDK | P7 |

## Phase 5: consumers of the engine

| Work | Decision |
|---|---|
| `Umpire.Testing`: unit test helpers and a definition test base, both through `LocalHost` | P7, Q7 |
| The tooling is a separate app, and `ConsoleHost` runs it | P7 |
| `umpire check` starts the engine version of the app | Q2 (watch) |

## End review

Uncertain points of this work. The user reviews them at the end of the plan, not during the work.

- Unify with the sibling design talk ("Design: host-agnostic app and entry points", draft spike #381, page https://claude.ai/artifact/F6jvJV9kdCe34Y2Jto2obD). Its "App and host split" (the app names only its kernel, the entry gives the host as a value, one fixed composition, an untyped `LocalHost.Invoke` in the core) differs from the "Entry design" of this plan (`EngineApp<THost>`). This plan continues with its own entry design until the unification pass.
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

## Later

- K5 (#344), with P11 (the scope of a rule).
- The typed channel of `LocalHost`.
- Own checks of an app (option C, maybe B).
- #380: a handler returns `Task<T>`.
- A check at start that each handler has a call, and a call that names its own handler base (#382 review).
- The Visual Studio test of the spike (needs the computer of the user).
- A full cleanup and review of the tests, at the end of this phase. It includes: the intermittent `CS8032` of `Rules.Tests` with the shared compiler server (seen three times), and the older path bugs that the #386 review found (a file on another drive counts as in the block, case in `InInnerBlock` and `RelativePath`, a folder name that starts with `..`, `OpenGenerics.NameOf` for a type nested in a generic type).
- vsa-unity, before a Unity release: the pipeline closes each handler call at run time with `MakeGenericType` and `Activator.CreateInstance` (#382). A call with a struct type argument, for example `CommandHandlerCall<X, int>`, can fail on an IL2CPP player if no code for that type exists at build time. Test a player with an `ICommand<int>` command.
- vsa-unity, at the next consumer update: `Get<T>` of `UmpireConfig` binds module settings with reflection (#388). Under IL2CPP stripping, a removed setter is skipped with no error. A Unity host keeps the setters (`link.xml`, `[Preserve]`) or adds its own `ISettingsProvider`.

## Limits

- Do not change the VSA rule lines before K5.
- Measure only, do not change: the size of a snapshot (about 7 KB for each type).
