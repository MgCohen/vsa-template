# Unity

The framework obeys the limits that Unity 6 gives, and a test checks each limit that the code can check. Where Unity must replace a part of the framework, the framework gives an extension point. The Unity parts themselves (IL2CPP, stripping, `link.xml`, the Unity host, the Unity build) are in the UPM package `vsa-unity`.

## Limits that the framework obeys

| Limit | Value | Set in | Test |
|---|---|---|---|
| Target of the portable projects | `netstandard2.0` only (one build for Unity, .NET apps, the engine and the compiler) | `Directory.Build.props` (`UmpirePortableLayer`) | `PortableTargetTests` |
| APIs of the portable projects | `netstandard2.0` only | The compiler, on the `netstandard2.0` target | The build |
| `Microsoft.Extensions` on `netstandard2.0` | 8.0 | `Directory.Packages.props` | `PortableReferenceTests` |
| Roslyn of the analyzers | 4.10 at most | The two analyzer project files | `AnalyzerRoslynTests` |
| Target of the analyzers | `netstandard2.0` | The two analyzer project files | `AnalyzerRoslynTests` |
| Compiler assemblies of the analyzers | SCI and `System.Reflection.Metadata` 8.0 at most, no other reference | The two analyzer project files, `Umpire.ConfigBundle.targets` | `AnalyzerRoslynTests` |
| The analyzers load and read `.umpireconfig` in the Unity compiler | .NET 8, Roslyn 4.10, SCI 8.0 | `Umpire.ConfigBundle.targets` | `AnalyzerLoadTests` |
| The engine runs on the .NET 8 of a Unity 6000.6 Editor | `net8.0`, `RollForward` `Major` | `Directory.Build.props` (`UmpireEngineLayer`) | `Net8EngineTests` (`UMPIRE_NET8_DOTNET_ROOT`) |
| Target of `Umpire.Markers` | `netstandard2.0` | `architecture/Markers/Umpire.Markers.csproj` | `PortableTargetTests` |

The tests are in `tests/Framework.Tests/Unity/`. They read the built DLLs in `artifacts~/bin/`, so `Framework.Tests` builds the `netstandard2.0` target of each portable project first. If you add a project to `UmpirePortableLayer`, also add it to `PortableProjects.cs` and to the table "Layers that Unity can use".

The reasons for the limits:

- Unity 6 ships some `Microsoft.Extensions` DLLs at version 8.0, in `Editor/Data/BCLExtensions`. A higher version is a compile error in Unity (`CS1705`).
- Roslyn 4.10 is the compiler of Unity 6. A newer compiler also runs an analyzer for 4.10. An analyzer that references a newer Roslyn does not run in Unity (`CS9057`).
- The compiler of Unity 6 runs on .NET 8 and owns `System.Collections.Immutable` 8.0. An analyzer that references 9.0 does not load (`CS8032`), and a copy of 9.0 in the analyzer folder breaks the analyzer API. So the kernel analyzer DLL embeds its config reader and loads it in a private load context (see "The config reader in the compiler" in `docs/architecture-rules.md`).
- The framework has no C# language limit. Unity does not compile the framework source: it gets the DLLs. The compiler refuses each feature that needs runtime support that `netstandard2.0` does not have, for example default interface methods. Unity compiles only the code in `Assets/`, with C# 9, and `vsa-unity` owns the rules for that code.

## Extension points

| Unity replaces | Extension point | Doc |
|---|---|---|
| The type source of the modules | `AppHost.ModuleProvider` (`IModuleProvider`, `FixedModules`, `ModuleScan.In`), `new AppTypes(provider)` | `docs/composition.md` |
| The host | `UnityHost : AppHost` with `Configure`: `App<VsaKernel, UnityHost>` | `docs/composition.md` |
| The lifecycle | The shell calls `app.Start(args)` and `app.Stop()`. It owns the frame loop | `docs/composition.md` |
| The host arguments | The base host reads only `--Key=Value` and `--Key value` of `args` into `HostArguments`, and it ignores other arguments (for example `-batchmode` or the player path). `UnityHost.Arguments(args)` can give other values | `docs/composition.md` |
| The container | `host.Compose(services, arguments, kernel, types)`, or a bridge that reads `App.Services` | `docs/composition.md` |
| The presenter scopes | The shell or the page resolves `PresenterScope` in its scope | `docs/presenter-events.md` |
| Objects that the container cannot make | `IFactory<T>`, `services.AddFactory` | `docs/composition.md` |
| The logger | An `ILoggerFactory` in `UnityHost.Configure`. The default is the null logger of `Microsoft.Extensions.Logging.Abstractions` | `docs/composition.md` |
| The module settings | `UnityHost.Settings(types)`: a provider in front of the default, for example one with no reflection | `docs/composition.md` |
| The source folders | `UmpireExcludedFolders`, `umpire.check.folder_aliases`, `config.FolderAliases`. `Assets/<App>/` binds as the block root | `docs/architecture-engine.md` |

## Vendored DLLs

Unity cannot restore NuGet packages. Each release has the archive `umpire-<version>.zip` on its GitHub release, with the `netstandard2.0` runtime, the two analyzer DLLs and the engine (see "Release archive" in `docs/consumers.md`). vsa-unity copies the DLLs from it into its package. The Umpire part is `Umpire.dll` (the core) and `Umpire.Vsa.dll` (the VSA kernel); the other DLLs are their third-party dependencies.

## Unity app build

The Umpire packages have no Unity build steps. The UPM package `vsa-unity` gives the Unity parts: the copy of the module DLLs to `Assets/Plugins/VSA/` and the .NET tests of the module sources in `Assets/` (MgCohen/vsa-unity#8). vsa-unity sets `UmpireExcludedFolders` (for example `Assets`), so that the build files of the package `Umpire` do not compile `Assets/`.

Each Umpire analyzer rule has the defaults of its values on its class, and a disabled rule is off on its class. The analyzers read the values of the block from `.umpireconfig`, which the compiler gets as an additional file. The Unity package must give that file to the Unity compiler as an additional file. Else the analyzers use the defaults. If a value is not valid, the analyzer reports `UMP010001`.

The VSA analyzer DLL needs the kernel analyzer DLL: its rules inherit the base types of `Umpire.Kernels.Analyzers`. Roslyn finds a dependency of an analyzer first in the folder of that analyzer. In another folder, it takes the first registered path with the same name and version, also a deleted one. So `Umpire.Vsa.Analyzers` holds both DLLs in `analyzers/dotnet/cs/` (pinned choice, `framework/Kernel/Analyzers/Umpire.KernelBundle.targets`). The kernel analyzer DLL embeds Umpire.Config. So a host that loads the analyzers itself puts the two DLLs in one folder. If the embedded config reader does not load, the build gives `UMP010003`. The package `Umpire` brings the package `Umpire.Kernels.Analyzers`, and the package `Umpire` keeps only the copy in `Umpire.Vsa.Analyzers` (`_UmpireOneKernelAnalyzer`), so `KernelAnalyzer` runs one time. A Unity host that vendors the archive gets one copy.

## Build output

An Umpire app writes all build output to `artifacts~/` in the app folder: `umpire new app` and `umpire init` write `UseArtifactsOutput` and `ArtifactsPath` in its `Directory.Build.props`. The Umpire packages do not set the output folder. A folder whose name ends with `~` is not source: the rule engine and `umpire init` do not read it.

| Need | Do this in `Directory.Build.props` |
|------|-------------------------------------|
| Another output folder | Set `ArtifactsPath` to that folder |
| Output next to each project (`bin/`, `obj/`) | Set `UseArtifactsOutput` to `false` |

The .NET SDK reads these two properties only before its own props: from `Directory.Build.props` or from the command line. A value in a project file gives `NETSDK1199`.

## Layers that Unity can use

| Layer | Package | Unity |
|---|---|---|
| Domain | `Umpire.Vsa.Domain` | Yes |
| Contracts (`IMessage`, `Envelope`, `Result`) | `Umpire.Messages.Contracts` | Yes |
| Handler pipeline | `Umpire.Messages.Handlers` | Yes |
| Contracts, commands and events | `Umpire.Vsa.Contracts.Commands`, `Umpire.Vsa.Contracts.Events` | Yes |
| Handlers, commands and events | `Umpire.Vsa.Handlers.Commands`, `Umpire.Vsa.Handlers.Events` | Yes |
| Infrastructure | `Umpire.Vsa.Infrastructure` | Yes |
| Kernel (`App`, `AppHost`, `Module`) | `Umpire.Kernels` | Yes |
| The `.umpireconfig` reader | `Umpire.Config` (its own TOML reader, no package dependency) | Yes. `Get<T>` binds the settings with reflection on the public properties |
| Composition | `Umpire.Vsa.Composition` | Yes, without presenters. `VsaKernel` and `Plugin` are here |
| Contracts, requests | `Umpire.Vsa.Contracts.Requests` | Yes. The operation members (`Group`, `Name`, `Effect`) are not on `netstandard2.0` |
| Handlers, requests | `Umpire.Vsa.Handlers.Requests` | Yes |
| Presenters | `Umpire.Vsa.Presenters` | Yes |
| Architecture definition | `Umpire.Architecture`, `Umpire.Kernels.Definition`, `Umpire.Vsa` | Only for a definition: no runtime type names them. The packages `Umpire.Definition` and `Umpire.Vsa.Definition` hold them |
| The core: the kernel, the config reader and the messages, in one DLL | `Umpire` (`tools/bundle/Umpire/`) | Yes. Unity vendors this DLL, not the projects above |
| The VSA kernel: each VSA layer above except the views, in one DLL | `Umpire.Vsa` (`tools/bundle/Vsa/Umpire.Vsa.Kernel.csproj`) | Yes. Unity vendors this DLL, not the projects above |
| Host; Views | ASP.NET Core packages | No: ASP.NET Core |

## Rules for the netstandard2.0 target

The portable projects target `netstandard2.0`, and not `netstandard2.1`, because the engine will run in the compiler as an analyzer. An analyzer must target `netstandard2.0`: the .NET Framework compiler of Visual Studio cannot load `netstandard2.1`. Unity loads `netstandard2.0` libraries.

- On `netstandard2.0`, the 8.0 `Microsoft.Extensions` packages depend on more packages, for example `Microsoft.Bcl.AsyncInterfaces`, `System.Memory`, `System.Buffers` and `System.Threading.Tasks.Extensions`. The portable projects add no direct reference to them.

- The archive holds each DLL of the runtime, also a DLL that Unity ships. vsa-unity does not copy a DLL that Unity ships: a second copy is a compile error. The manifest gives the version of each DLL.
- `netstandard2.0` does not have `IsExternalInit`. The layers compile `tools/bundle/Umpire/build/Polyfills~/` for that target (the name ends with `~`, so the engine does not count the file as a source of the block). The polyfill is internal, so each Unity assembly with a record has its own copy. The package `Umpire` also gives the polyfill to each `netstandard2.0` and `netstandard2.1` project of an app. A project with its own `IsExternalInit` sets `UmpireIsExternalInitPolyfill` to `false`.
- `AppTypes` finds the modules in the loaded assemblies, so a Unity app gives no modules. The Unity shell calls `app.Start(args)` and `app.Stop()`, and an app with its own container calls `host.Compose` (`docs/composition.md`).

The Umpire DLLs reference only these `Microsoft.Extensions` DLLs:

| DLL | Version | Why |
|---|---|---|
| `Microsoft.Extensions.DependencyInjection` | 8.0 | The container |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 8.0 | `IServiceCollection` and `IServiceProvider` |
| `Microsoft.Extensions.Logging.Abstractions` | 8.0 | `ILogger`. Unity ships it |

The Umpire DLLs do not reference `Microsoft.Extensions.Configuration`, `Logging`, `Options`, `Primitives` or `Hosting` (`KernelReflectionTests`, `ArchiveTests`).

## Non-goals

- No IL2CPP, stripping, `link.xml` or Unity reflection design in this repository. Reason: `vsa-unity` owns the Unity build, and it can test these limits in a Unity player.
