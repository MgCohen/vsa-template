# Open questions for the user

## K4: config, DLLs, packages (#331, #247)

The user has this list and did not answer it. An opinion is not a decision.

### Answers

- **Q1 (2026-11-25).** "No definition code on umpire DLL. The definition code will most likely need the umpire DLL, so the helpers can stay there, or we can move them into a utility." Rule of the user: instead of one using the other, move shared things to a utility.
- **Q2 and Q6 (2026-11-25).** "We can keep splitting": `Umpire` (the core), `Umpire.Definition` (the core definition), `Umpire.Vsa` (the host/kernel), `Umpire.Vsa.Definition` (the VSA definition). This also answers Q6: the core and VSA are two DLLs.
- **Q3 (2026-11-25).** Not answered. "We still have to talk about the topology and architecture and split of this, because I don't like the idea of check model. ... But it can live wherever it needs to live." New open item: the structure of the check code, before its place.
- **Q4 (2026-11-25).** "Our own reader, even if inside our DLL it uses Tomlyn or whatever we define. No one needs to know that it's behind our own interface, our own utility. But we can try to go with our own small one. Let's see how that plays out."
- **Q5 (2026-11-25).** "Remove as much as possible. The only thing that we really keep is the dependency injection, because it's the base of our work." First "fully understand why configuration was there and what we lose by removing it."
- **Q7 (2026-11-25).** Unity consumers use UPM, so they are not part of this question. For other C# consumers: "Why not just keep using GitHub Packages today? And then migrate to NuGet when it's ready." Decision: GitHub Packages now, nuget.org when the package set is final. A static feed is ranked down: it fixes only the token, and no consumer outside the user needs that now. Come back if an outside user must try Umpire before nuget.org.

| # | Question | Options | Opinion |
|---|---|---|---|
| 1 | What does the runtime ship? | `Umpire.dll` with no definition code, or with it | No definition code (D1 says it) |
| 2 | Where do the definition API and the pack definitions ship, and for which target (now `net10.0`)? | One package, one for each pack, or in the SDK | One core package, and one for each pack |
| 3 | Where does the check model ship, when the analyzer and the command use it? | A copy in each, or one shared DLL | A copy in each: they load DLLs differently |
| 4 | Config reader | Own small TOML reader, Tomlyn, or JSON | Own small reader: no third-party DLL in the analyzer |
| 5 | Other third-party DLLs in the runtime (#247) | Step 3 only, or steps 2 and 3 | Step 3 first: step 2 changes the public API |
| 6 | Core and VSA in two DLLs (#331) | One or two | One, until a second pack ships |
| 7 | Public feed (#221), Unity package (#294) | Now or later | Later |

Also in K4: the place of `LoadedTypes` and `ClosedBase`, which `Umpire.Kernels` uses from `Umpire.Architecture`.

## K3 remainder

- Is the definition plus snapshot data one artifact or many?
- The depth of each operation.
- Term clashes (Q9 of the check goals page).
- Calls for managed files.

## Check model

- Which path does the mark of a DLL use? A path in `bin` is absent after a clean.
- Must a package with no library file give a finding?
- Can the SDK copy the `System.*` and `Microsoft.Extensions.*` DLLs of a definition? Today it does not.
- A file that another process adds between the build start and the compile is new only at the next compile. Is this limit acceptable?

## Warning default (#377): known limits

- The engine gives exit code 1 for findings and for bad arguments. A step that throws is a warning with exit code 0 in the mode `Warn`. The check command has the four codes.
- A mode value that is not valid gives `Warn` with no finding. Is a finding necessary?
- The build targets read `umpire.check.mode` with a regular expression, only for `UMP030003` and `UMP030004`. Remove it with the switch to the new check.
- `UMP030513` does not run if the definition of the block does not load.
- Engine setup warnings (for example `UMP030304`) stay warnings in this repository.
- The reviewer did not read the corrections of its findings.

## Link reversed

- If an inner pack definition does not load and the app names a base kernel with its own definition, there is no finding.
- Agent choices that the user can refuse: an inner pack with a kernel that the app does not use is no error. A kernel of another pack, in a project with no app, turns the VSA rules of that project off.
- File names of `VsaArchitecture` (the generic and the normal class).

## VSA, later

- Remove `rules.For<App>(...).And<DefinitionRoot>()` and the `Host` line. Opinion: yes.
- Permit `For<Definition>().Uses<Pack>()` for an app definition on its own pack variant. Opinion: yes.
- The kernel rules: the strictness of `Views` and `Composition`, and the uses of `Core` and `Messages` (#364).

## Check system topology (page "Check System Topology")

The proposals and the questions of https://claude.ai/artifact/GQkEAJ48rdzvsDjPtXfBgf. Each answer is in the words of the user.

### Answers

- **P1 (2026-11-25).** Accepted. "I want it to be unified. We only have one thing, the engine, period. If we have our own analyzers, they're inside the engine. There might be different entry points, but they all go through the same system."
- **P1, analyzers (2026-11-25).** Option A: all our analyzers are in the engine, also the generic rules. "Everything goes through the same place. They might have some extra utility. But every code that we have that checks, no matter what, goes through the same system. They get the config from the same place. We only have one point of maintenance." This changes K3: generic rules are engine operations that need no app model, not analyzers outside the engine.
- **P2 (2026-11-25).** One project, not two. "I'm fine with the separation in the internal folder structure, the internal code organization. It is decoupled, but we don't need two projects if it's not needed." Target `netstandard2.0`, if it works everywhere: only the TOML reader needed more, and we replace it. The analyzers do no I/O of their own: "They just trigger the engine, and the engine reads all the necessary files on its own. The only thing they might do is send the symbols." The flow: the engine has the saved state of all projects. At the start of a compilation, the analyzer sends the symbols of its project, and the engine updates that one entry. Then the analyzer checks type by type, and it updates each type fact when necessary, so the check is incremental.
- **P3 and Q1, terms (2026-11-25).** The levels: job, operation, rule. "A job is the whole thing, the operation is the second thing: an operation is checking architecture. One line of the architecture is one check or one rule." A job is one call of the engine: one input, one output. "We define jobs by using the standard VSA handler, very similar to what the engine had before. A job is one request, and the handler of that job calls several operations." An operation has its own logic: "if it needs to call checks or do the business logic that belongs to it, fine, that is scoped to the operation." Thus the K2 "jobs" (format, code check, architecture check, setup check, unit testing) are operations. The runner does not find jobs by type: each job handler names its operations, as `Run` does today.
- **Entry design (2026-11-26).** The analyzer triggers a job like any other caller of the app, with no knowledge of the container. The engine is host agnostic: `EngineApp<THost> : App<VsaKernel, THost>`, and each entry closes the host at definition (`EngineApp<ConsoleHost>` for the command line, `EngineApp<LocalHost>` for the analyzer). `AppHost` stays the base with no transport. `LocalHost` is the transport of local method calls: it exposes the requests as `app.Send(request)`, through an extension method that exists only for an app with `LocalHost`. The host stays private in `App`: the extension uses an `internal` member, nothing public. "I like the idea of exposing through an extension and not making it public, just internal." The runtime moves to `netstandard2.0` (the analyzer needs it), after a check of the APIs. Spike first: a one-request app in an analyzer, in `dotnet build` and in Visual Studio. Separate item: make `App.Services` internal or protected, after a check of its users.
- **P4 (2026-11-26).** Accepted with a change. Export is an operation. Report is not an operation: "Each operation can only fill the report. The job creates a report, the operations fill it, and the report goes back to whoever called the job. Whoever calls it does whatever it wants with it. So maybe we don't need specific job return types." Thus each job returns the same report type, and each caller (analyzer, console host, build) converts it.
- **P5 (2026-11-26).** Accepted "for now", with two plugins: Jobs (the pipeline parts of the jobs) and Facts (the data readers, `IFactSource<T>`). Outputs is not a plugin, because the callers convert the report (P4). Also: issue #380 (a handler returns `Task<T>`, and the pipeline makes the `Result<T>`).
- **P6 (2026-11-26).** Accepted for now. "We should strive for the minimal amount, but whatever is needed." `config.json` goes: the engine reads `.umpireconfig` with its own reader. The user is not sure that `block.json` is necessary. Still open: the source of the snapshot, and the facts of the DLL types (Q8).
- **P7, packages (2026-11-26).** Partly answered.
  - Targets: each package targets the lowest framework that its code needs. Do not keep a target from an old decision (for example the `net10.0` default). The engine, the runtime and the definitions target `netstandard2.0`. `Umpire.Web` needs ASP.NET Core, so `net8.0`. `Umpire.Console` can be `netstandard2.0` if SIGTERM works with `AppDomain.ProcessExit`.
  - "The CLI is the same as web and console, it's just a host." The tooling (scaffolding) is a separate thing on its own layer, and the console host runs it.
  - The engine ships as one thing: "Just one thing. The minimum, the better." One package, `Umpire.Engine`, with the analyzer entry and the console entry.
  - The SDK goes. A consumer uses `Microsoft.NET.Sdk` and package references, and the packages bring their own `build/` props and targets. The output folder uses the standard .NET switch `UseArtifactsOutput` in `Directory.Build.props`: this repository keeps it, and a consumer can add it (`umpire init`). The engine state files use the intermediate output path, not `artifacts~/`.
  - `Umpire.Testing` (option A): one test package for consumers. It holds the unit test helpers (the test starts the app with `LocalHost` and sends requests) and a thin definition test base that calls the engine through `LocalHost`. No copy of the rules. This also answers Q7. The user: the design is for consumers too, for example a game that uses the engine to validate VSA and adds its own tests and rules.
- **Own checks of an app (2026-11-26).** Option A now: an app adds only rule lines, and a special case is a normal unit test. Later option C (a new rule line kind and its operation in a pack variant of the app), "potentially even B" (an operation class in the `Definition/` project of the app). K2 and #143 hold for now.
- **P8 (2026-11-26).** Option A: the shared code (config reader, `LoadedTypes`, `ClosedBase`, text file writer, version check) is a utility folder and namespace inside `Umpire`. No new package, DLL or source copy. Helpers that must not be public are `internal`, with `InternalsVisibleTo`.
- **P9, Q3, Q5 (2026-11-26).** Accepted for now: four jobs by trigger (analyzer job, build job, check job, check job with tests). "We might need to tweak the analyzer job later as we see it running." No unit tests in `dotnet build` (Q3). Format runs `dotnet format --verify-no-changes` (Q5). The name "edit job" goes, because it sounds like a job that edits.
- **Job names (2026-11-26).** Each job has the name of its trigger: compile job (the analyzer), build job, check job.
