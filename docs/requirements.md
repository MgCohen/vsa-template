# Requirements of the check system

This file is the baseline for each design decision about the check system of Umpire: the engine, the checks, the definition, the tools, and how they ship. It is not a feature list.

Use it to judge an idea:

| Group | Effect on an idea |
|---|---|
| **Must** | An idea that breaks it is rejected |
| **Want** | It ranks an idea up |
| **Avoid** | It ranks an idea down. It rejects an idea only if the item says "never". |
| **Guideline** | It shows how we lean. It does not pass or fail an idea. |

An idea that breaks no Must is not rejected. It is ranked down, and it stays on record with its reason and a "come back if" note (G6).

## Must

| Id | Requirement | Reason |
|---|---|---|
| R1 | Each part of Umpire targets the runtime of the host where it runs | See "Runtime targets" below |
| M1 | Each finding is in the build output that an agent reads | Agents work from the output of `dotnet build`. A check that does not write to the build output does not exist for an agent. |
| M2 | One system runs and reports all checks, through one entry point | Users and agents start one check and get one result. Other tools can run, but only under that entry. |
| M3 | One architecture definition is the only source of truth | The definition holds the layers, parts and rules. Every other form (project files, tests, reports, a compiled definition file) comes from it. |
| M5 | The system finds each module, slice and class with no manifest | A list or an attribute for each instance goes out of date. The system finds instances from folders, namespaces and base types. |
| M6 | C# and .NET for all tools, with other languages only where a host forces them | One language keeps the stack small: C#, the .NET CLI, Blazor, JSON Schema, NUnit. |
| M7 | A consumer adds layers, bases and rules without edits to the build setup | An app extends the architecture in C# only. A layer map in MSBuild broke when consumers extended it. |
| M9 | The core and the engine stay generic; pack ideas stay in packs | Slices, features and other VSA names belong to the VSA pack. The core and the engine must work for another pack too. |
| M10 | The engine passes its own architecture check | The engine is a VSA console app, so it obeys the rules that it checks. |
| M16 | Each file other than source code is generated or managed by the engine | The engine can keep only its own files correct. A file that another tool owns, with one Umpire flag added, is not enough. |
| W9 | Each option value comes from one fixed lookup order | The call site, then the concept, then the architecture, then the default. Each place can leave the value empty, and then the next place answers. |
| W10 | The definition is typed, readable code with no strings | Types give compile errors and rename support. The layer map is data. A string match for a data-driven definition comes later. |

## Want

| Id | Desire | Reason |
|---|---|---|
| W1 | A small footprint in a consumer app: no file, or one line in a file that .NET already requires | Each Umpire file in an app is a cost. Files in this repository that ship inside the DLL are fine. |
| W4a | Fast feedback: a check run adds little build time | An agent waits for each build |
| W4b | Complete feedback: all errors in one run | An agent fixes all findings together, and does not run again for each one |
| W7 | All checks share one API shape and pattern; their parameters can differ | A new check is small, and all checks read the same way |
| W11 | Tool data is easy to parse, with a fixed shape for each input and output | Agents and tools read the data |
| W12 | Few shipped files: the core `Umpire.dll`, one DLL for each pack (`Umpire.Vsa.dll`) and one to five app outputs | Few files are easier to vendor, mainly in Unity |
| W19 | A Unity user sees the findings in the Unity Console | Code rules show natively in the Unity compiler. Architecture findings reach the Console through the vsa-unity runner. |
| W20 | A rule can check any property of the code that an architecture needs | Folder, path, folder structure, namespace, symbols, type names, file data, project or assembly. Each architecture chooses, and it can mix them: VSA checks folders, another architecture can check projects. |

## Avoid

| Id | Thing | Reason |
|---|---|---|
| A5 | A core API call that serves only one pack concept | The core stays generic (M9) |
| A7 | Umpire facts in shared .NET management files (`global.json`, `Directory.Build.props`, `nuget.config`) | These files belong to the whole repository, so an Umpire entry there is the worst footprint |
| A10 | Rule IDs or a text DSL in the definition data | The definition data holds only the definition |

## Guidelines

| Id | Guideline |
|---|---|
| G1 | Agents first, humans always: each output is readable and visible to people, but no agent flow breaks for human comfort |
| G2 | Unity in mind, not Unity-specific: this repository keeps the Unity limits and gives extension points. Unity design (views, IL2CPP, Unity-only files) belongs to vsa-unity. |
| G6 | Reject an idea only if it breaks a Must; else rank it down and keep it with its reason |
| W3 | Find things in this order: automatically (base type, interface, folder), then by a small declaring type, then by one central list of pack defaults, then by an attribute on each class |
| W5 | A finding message says what is wrong, the type names, and the fix |
| M13 | Do not remove a check to reduce the file count or the start time. Solve those at publish time, for example with one bundled DLL. |

## Runtime targets (R1)

A part builds for the lowest version of all its hosts. The lowest supported host sets each limit: today Unity 6000.6.

### What each part expects

| Part | Runs in | Target and limits | Reason |
|---|---|---|---|
| Runtime (`Umpire.dll`) | Unity player, Unity Editor, .NET apps, the engine, the compiler | `netstandard2.0` only. `Microsoft.Extensions` 8.0 at most. | One build for all hosts (#301). Unity 6000.6 ships Microsoft.Extensions 8.0. |
| Web host and views (`Umpire.Web`) | .NET apps | `net9.0` | `Views.Map` of Umpire.Hosts.Web calls `MapStaticAssets`, an API of ASP.NET Core 9. Umpire.Vsa.Views is in the same bundle, so it targets `net9.0` too |
| Console host (`Umpire.Console`) | .NET apps, the engine | `net8.0` | `PosixSignalRegistration` stops the app on SIGTERM. `AppDomain.ProcessExit` is not a replacement: on .NET 10 on Linux, SIGTERM stops the process with exit code 143, and the runtime does not raise `ProcessExit` |
| Analyzers | The C# compiler: .NET SDK, Visual Studio, Unity | `netstandard2.0`, Roslyn 4.10, `System.Collections.Immutable` and `System.Reflection.Metadata` 8.0 | The Unity 6000.6 compiler is Roslyn 4.10 on .NET 8 (`CS9057`, `CS8032`) |
| Engine | The .NET SDK after the build, the .NET of the Unity Editor | `net8.0` with `RollForward` Major. The build hook uses `LatestMajor`. | The Unity 6000.6 Editor has only .NET 8. The engine loads the app definition, so it runs on the newest runtime. |
| Test package (`Umpire.Testing`) | The test projects of an app | `net8.0` | NUnit 4 has no `netstandard2.0` build. No ASP.NET Core framework reference: the app of a test gets the null logger of the composition |
| Tool (`umpire`, with `Umpire.Files`) | The .NET SDK | `net8.0` with `RollForward` Major | The tool runs on .NET 8. If .NET 8 is not installed, it runs on the newest runtime, for example the runtime of the .NET 10 SDK that `dotnet tool exec` needs |
| Repository tests and internal tools | The .NET SDK | `net10.0`. The analyzer load tests also need a .NET 8 SDK (#289). | Only developers and CI run them |
| Consumer app, .NET | The .NET SDK | Any current .NET. A module that a Unity app also uses stays on `netstandard2.0` or `netstandard2.1`. | One module, all hosts |
| Consumer app, Unity | The Unity compiler | Code in `Assets`, C# 9. The DLLs come from the release archive. | No MSBuild and no NuGet restore in Unity |

### What each host gives

| Item | .NET SDK | Unity 6000.6.6f1 | Unity 6000.7.0b9 (beta) |
|---|---|---|---|
| .NET runtime | The installed one (10) | 8.0.21 | 10.0.11 |
| C# compiler (Roslyn) | The SDK one | 4.10 | 5.6 |
| Microsoft.Extensions shipped | Any, from NuGet | 8.0 | 10.0 |
| Player API | — | .NET Standard 2.1, C# 9 | .NET Standard 2.1, C# 9 |
| MSBuild and NuGet | Yes | No | Not examined |

If support for Unity 6000.6 ends, the analyzer, engine and Microsoft.Extensions limits can rise. See `docs/unity.md` for the tests of each limit.

## Out of scope

These topics have their own place:

| Topic | Place |
|---|---|
| Code style (comments, tuples, `global::`, names) | `AGENTS.md`, `.editorconfig` |
| VSA pack opinions (slices, features) | `docs/slice-layout.md`, `docs/architecture-rules.md` |
| Runtime composition (hosts, containers, modules) | `docs/composition.md` |
| How the sessions work | The agent notes |
