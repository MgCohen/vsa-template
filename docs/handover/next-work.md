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
| Fix the test failures of `main` first (bug issue) | Clean base |
| Take over draft PR #382: no default interface methods, one handler call for each kind | DIM decision |
| Move the portable layer to `netstandard2.0`. Fix each blocker from the spike | P2, entry design |
| Fix `AppClass.KernelsOf`: refuse only an open kernel, so an app class with an open host is found | Spike, assumption 4 |
| The analyzer entry gets a fixed module list and its own `.umpireconfig` path | Spike finding |
| `LocalHost : AppHost` and the `Send` extension, only for an app with `LocalHost`. The host stays internal | Entry design |
| `LocalHost` uses the `Operation` path (object, reflection) | LocalHost channel |
| A utility folder and namespace in `Umpire`: `LoadedTypes`, `ClosedBase`, the file writer, the version check | Q1, P8 |
| Our own TOML reader behind our own interface | K4 Q4 |
| Make `App.Services` internal or protected, after a check of its users | Entry design |

## Phase 2: packages

| Work | Decision |
|---|---|
| Split the runtime and the definition: `Umpire.dll` ships no definition code | K4 Q1, Q2 |
| Split core and VSA into two DLLs | K4 Q6 |
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
| Remove `UMP010206`. Its ID is not used again | Brief D item 5 |
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

## Later

- K5 (#344), with P11 (the scope of a rule).
- The typed channel of `LocalHost`.
- Own checks of an app (option C, maybe B).
- #380: a handler returns `Task<T>`.
- A check at start that each handler has a call, and a call that names its own handler base (#382 review).
- The Visual Studio test of the spike (needs the computer of the user).

## Limits

- Do not change the VSA rule lines before K5.
- Measure only, do not change: the size of a snapshot (about 7 KB for each type).
