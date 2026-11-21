# Unity

The framework obeys the limits that Unity 6 gives, and a test checks each limit that the code can check. Where Unity must replace a part of the framework, the framework gives an extension point. The Unity parts themselves (IL2CPP, stripping, `link.xml`, the Unity host, the Unity build) are in the UPM package `vsa-unity`.

## Limits that the framework obeys

| Limit | Value | Set in | Test |
|---|---|---|---|
| Target of the portable projects | `net8.0` and `netstandard2.1` | `Directory.Build.props` (`UmpirePortableLayer`) | `PortableTargetTests` |
| APIs of the portable projects | `netstandard2.1` only | The compiler, on the `netstandard2.1` target | The build |
| `Microsoft.Extensions` on `netstandard2.1` | 8.0 | `Directory.Packages.props` | `PortableReferenceTests` |
| Roslyn of the analyzers | 4.10 at most | The two analyzer project files | `AnalyzerRoslynTests` |
| Target of the analyzers | `netstandard2.0` | The two analyzer project files | `AnalyzerRoslynTests` |
| Compiler assemblies of the analyzers | SCI and `System.Reflection.Metadata` 8.0 at most, no other reference | The two analyzer project files, `Umpire.ConfigBundle.targets` | `AnalyzerRoslynTests` |
| The analyzers load and read `.umpireconfig` in the Unity compiler | .NET 8, Roslyn 4.10, SCI 8.0 | `Umpire.ConfigBundle.targets` | `AnalyzerLoadTests` |
| The engine runs on the .NET 8 of a Unity 6000.6 Editor | `net8.0`, `RollForward` `Major` | `Directory.Build.props` (`UmpireEngineLayer`) | `Net8EngineTests` (`UMPIRE_NET8_DOTNET_ROOT`) |
| Target of `Umpire.Markers` | `netstandard2.0` | `architecture/Markers/Umpire.Markers.csproj` | `PortableTargetTests` |

The tests are in `tests/Framework.Tests/Unity/`. They read the built DLLs in `artifacts~/bin/`, so `Framework.Tests` builds the `netstandard2.1` target of each portable project first. If you add a project to `UmpirePortableLayer`, also add it to `PortableProjects.cs` and to the table "Layers that Unity can use".

The reasons for the limits:

- Unity 6 ships some `Microsoft.Extensions` DLLs at version 8.0, in `Editor/Data/BCLExtensions`. A higher version is a compile error in Unity (`CS1705`).
- Roslyn 4.10 is the compiler of Unity 6. A newer compiler also runs an analyzer for 4.10. An analyzer that references a newer Roslyn does not run in Unity (`CS9057`).
- The compiler of Unity 6 runs on .NET 8 and owns `System.Collections.Immutable` 8.0. An analyzer that references 9.0 does not load (`CS8032`), and a copy of 9.0 in the analyzer folder breaks the analyzer API. So the kernel analyzer DLL embeds its config reader and loads it in a private load context (see "The config reader in the compiler" in `docs/architecture-rules.md`).
- The framework has no C# language limit. Unity does not compile the framework source: it gets the DLLs. The compiler refuses each feature that needs runtime support that `netstandard2.1` does not have. Unity compiles only the code in `Assets/`, with C# 9, and `vsa-unity` owns the rules for that code.

## Extension points

| Unity replaces | Extension point | Doc |
|---|---|---|
| The type source of the modules | `AppHost.ModuleProvider` (`IModuleProvider`, `FixedModules`, `ModuleScan.In`), `new AppTypes(provider)` | `docs/composition.md` |
| The host | `UnityHost : AppHost` with `Configure`: `App<Vsa<UnityHost>>` | `docs/composition.md` |
| The lifecycle | The shell calls `app.Start(args)` and `app.Stop()`. It owns the frame loop | `docs/composition.md` |
| The configuration | The base host reads only `--Key=Value` and `--Key value` of `args` into `IConfiguration`, and it ignores other arguments (for example `-batchmode` or the player path). `UnityHost.Configuration(args)` can add other sources | `docs/composition.md` |
| The container | `host.Compose(services, configuration, kernel, types)`, or a bridge that reads `App.Services` | `docs/composition.md` |
| The presenter scopes | The shell or the page resolves `PresenterScope` in its scope | `docs/presenter-events.md` |
| Objects that the container cannot make | `IFactory<T>`, `services.AddFactory` | `docs/composition.md` |
| The logger providers | `AddLogging` on the given `IServiceCollection` | `docs/composition.md` |
| The module settings | `UnityHost.Settings(types)`: a provider, or a Tomlyn context for no reflection | `docs/composition.md` |
| The source folders | `UmpireExcludedFolders`, `umpire.check.folder_aliases` (and `FolderAliases` in code) | `docs/architecture-engine.md` |

## Vendored DLLs

Unity cannot restore NuGet packages. The Unity repo keeps the DLLs in git, and `vendor.cs` writes them: it publishes a `netstandard2.1` project with the package `Umpire`, then copies the DLLs to `Assets/Plugins/VSA`. The Umpire part is one `Umpire.dll`; the other DLLs are its third-party dependencies.

## Unity app build

Umpire.Sdk has no Unity build steps. The UPM package `vsa-unity` gives the Unity parts: the copy of the module DLLs to `Assets/Plugins/VSA/` and the .NET tests of the module sources in `Assets/` (MgCohen/vsa-unity#8). vsa-unity sets `UmpireExcludedFolders` (for example `Assets`), so that Umpire.Sdk does not compile `Assets/`.

Each Umpire analyzer rule has the defaults of its values on its class, and a disabled rule is off on its class. The analyzers read the values of the block from `.umpireconfig`, which the compiler gets as an additional file. The Unity package must give that file to the Unity compiler as an additional file. Else the analyzers use the defaults. If a value is not valid, the analyzer reports `UMP010001`.

The VSA analyzer DLL needs the kernel analyzer DLL: its rules inherit the base types of `Umpire.Kernels.Analyzers`. Roslyn finds a dependency of an analyzer first in the folder of that analyzer. In another folder, it takes the first registered path with the same name and version, also a deleted one. So `Umpire.Vsa.Analyzers` holds both DLLs in `analyzers/dotnet/cs/` (pinned choice, `framework/Kernel/Analyzers/Umpire.KernelBundle.targets`). The kernel analyzer DLL embeds Umpire.Config, Tomlyn and their dependencies. So a host that loads the analyzers itself puts the two DLLs in one folder. If the embedded config reader does not load, the build gives `UMP010003`. Umpire.Sdk does not add the Umpire.Kernels.Analyzers package in package mode, so `KernelAnalyzer` runs one time.

## Build output

An app on the Umpire SDK writes all build output to `artifacts~/` in the block root: the folder of the nearest `.umpireconfig`. The SDK sets `UseArtifactsOutput` and `ArtifactsPath` before the .NET SDK, because it is the main SDK of each project. A folder whose name ends with `~` is not source: the rule engine and `umpire init` do not read it.

| Need | Do this in `Directory.Build.props` |
|------|-------------------------------------|
| Another output folder | Set `ArtifactsPath` to that folder |
| Output next to each project (`bin/`, `obj/`) | Set `UseArtifactsOutput` to `false` |

The .NET SDK reads these two properties only before its own props: from the Umpire SDK, from `Directory.Build.props` or from the command line. A value in a project file gives `NETSDK1199`.

## Layers that Unity can use

| Layer | Package | Unity |
|---|---|---|
| Domain | `Umpire.Vsa.Domain` | Yes |
| Contracts (`IMessage`, `Envelope`, `Result`) | `Umpire.Messages.Contracts` | Yes |
| Handler pipeline | `Umpire.Messages.Handlers` | Yes |
| Contracts, commands and events | `Umpire.Vsa.Contracts.Commands`, `Umpire.Vsa.Contracts.Events` | Yes |
| Handlers, commands and events | `Umpire.Vsa.Handlers.Commands`, `Umpire.Vsa.Handlers.Events` | Yes |
| Infrastructure | `Umpire.Vsa.Infrastructure` | Yes |
| Kernel (`App`, `Pack`, `AppHost`, `Module`) | `Umpire.Kernels` | Yes |
| The `.umpireconfig` reader | `Umpire.Config` (Tomlyn, its `netstandard2.0` asset) | Yes. Tomlyn needs `System.Buffers`, `System.Collections.Immutable` and `System.Text.Json` 10 |
| Composition | `Umpire.Vsa.Composition` | Yes, without presenters. `VsaKernel` and `Plugin` are here |
| Contracts, requests | `Umpire.Vsa.Contracts.Requests` | Yes. The operation members (`Group`, `Name`, `Effect`) are only on `net8.0` |
| Handlers, requests | `Umpire.Vsa.Handlers.Requests` | Yes |
| Presenters | `Umpire.Vsa.Presenters` | Yes |
| Architecture definition | `Umpire.Architecture`, `Umpire.Vsa` | Yes. `Pack` needs them, and `Vsa<THost>` is here |
| All the parts above, in one DLL | `Umpire` (`tools/bundle/`) | Yes. Unity vendors this DLL, not the projects above |
| Host; Views | ASP.NET Core packages | No: ASP.NET Core |

## Rules for the netstandard2.1 target

- `vendor.cs` does not copy a DLL that Unity ships. A second copy is a compile error.
- `netstandard2.1` does not have `IsExternalInit`. The layers compile `tools/sdk/Polyfills/` for that target. The polyfill is internal, so each Unity assembly with a record has its own copy.
- `AppTypes` finds the modules in the loaded assemblies, so a Unity app gives no modules. The Unity shell calls `app.Start(args)` and `app.Stop()`, and an app with its own container calls `host.Compose` (`docs/composition.md`). The Umpire DLLs reference `Microsoft.Extensions.DependencyInjection`, `Logging`, `Configuration` and `Configuration.CommandLine` 8.0, and not `Microsoft.Extensions.Hosting`. `vendor.cs` must copy `Microsoft.Extensions.Configuration.CommandLine.dll` if Unity does not ship it.

## Non-goals

- No IL2CPP, stripping, `link.xml` or Unity reflection design in this repository. Reason: `vsa-unity` owns the Unity build, and it can test these limits in a Unity player.
