# Unity

The framework obeys the limits that Unity 6 gives, and a test checks each limit that the code can check. Where Unity must replace a part of the framework, the framework gives an extension point. The Unity parts themselves (IL2CPP, stripping, `link.xml`, the Unity host, the Unity build) are in the UPM package `vsa-unity`.

## Limits that the framework obeys

| Limit | Value | Set in | Test |
|---|---|---|---|
| Target of the portable projects | `net10.0` and `netstandard2.1` | `Directory.Build.props` (`UmpirePortableLayer`) | `PortableTargetTests` |
| APIs of the portable projects | `netstandard2.1` only | The compiler, on the `netstandard2.1` target | The build |
| `Microsoft.Extensions` on `netstandard2.1` | 8.0 | `Directory.Packages.props` | `PortableReferenceTests` |
| Roslyn of the analyzers | 4.10 at most | The two analyzer project files | `AnalyzerRoslynTests` |
| Target of the analyzers | `netstandard2.0` | The two analyzer project files | `AnalyzerRoslynTests` |
| Target of `Umpire.Markers` | `netstandard2.0` | `architecture/Markers/Umpire.Markers.csproj` | `PortableTargetTests` |

The tests are in `tests/Framework.Tests/Unity/`. They read the built DLLs in `artifacts~/bin/`, so `Framework.Tests` builds the `netstandard2.1` target of each portable project first. If you add a project to `UmpirePortableLayer`, also add it to `PortableProjects.cs` and to the table "Layers that Unity can use".

The reasons for the limits:

- Unity 6 ships some `Microsoft.Extensions` DLLs at version 8.0, in `Editor/Data/BCLExtensions`. A higher version is a compile error in Unity (`CS1705`).
- Roslyn 4.10 is the compiler of Unity 6. A newer compiler also runs an analyzer for 4.10. An analyzer that references a newer Roslyn does not run in Unity (`CS9057`).
- The framework has no C# language limit. Unity does not compile the framework source: it gets the DLLs. The compiler refuses each feature that needs runtime support that `netstandard2.1` does not have. Unity compiles only the code in `Assets/`, with C# 9, and `vsa-unity` owns the rules for that code.

## Extension points

| Unity replaces | Extension point | Doc |
|---|---|---|
| The type source of the modules | `App.Modules()`, `new AppTypes(modules)` | `docs/composition.md` |
| The host | The host type argument: `App<Vsa<UnityVsaHost>>` | `docs/composition.md` |
| The container | `CreateHost().Compose(services, app)`, with no `Run` | `docs/composition.md` |
| Objects that the container cannot make | `IFactory<T>`, `services.AddFactory` | `docs/composition.md` |
| The logger providers | `AddLogging` on the given `IServiceCollection` | `docs/composition.md` |
| The source folders | `UmpireExcludedFolders`, `FolderAliases` | `docs/architecture-engine.md` |

## Vendored DLLs

Unity cannot restore NuGet packages. The Unity repo keeps the DLLs in git, and `vendor.cs` writes them: it publishes a `netstandard2.1` project with the VSA packages, then copies the DLLs to `Assets/Plugins/VSA`.

## Unity app build

Umpire.Sdk has no Unity build steps. The UPM package `vsa-unity` gives the Unity parts: the copy of the module DLLs to `Assets/Plugins/VSA/` and the .NET tests of the module sources in `Assets/` (MgCohen/vsa-unity#8). Umpire.Sdk does not compile `Assets/` (`UmpireExcludedFolders`).

Each Umpire analyzer rule has the defaults of its values on its class. Two globalconfig files hold the disabled severities and the help text: `framework/Kernel/Analyzers/Umpire.Kernel.Analyzers.globalconfig` and `packs/Vsa/Tests/Analyzers/Umpire.Vsa.Analyzers.globalconfig`. Each analyzer package ships its file as `build/<PackageId>.globalconfig`, and `Umpire.Vsa.Analyzers` also ships the kernel file. Unity reads a global config only as `Assets/Default.globalconfig` or `<AsmdefName>.globalconfig`, so the Unity package must copy both files there. If a value is not valid, the analyzer reports `UMP010001`.

The VSA analyzer DLL needs the kernel analyzer DLL: its rules inherit the base types of `Umpire.Kernel.Analyzers`. Roslyn finds a dependency of an analyzer first in the folder of that analyzer. In another folder, it takes the first registered path with the same name and version, also a deleted one. So `Umpire.Vsa.Analyzers` holds both DLLs in `analyzers/dotnet/cs/` and both values files in `build/` (pinned choice, `framework/Kernel/Analyzers/Umpire.KernelBundle.targets`). A Unity project puts both DLLs in one folder. Umpire.Sdk does not add the Umpire.Kernel.Analyzers package in package mode, so `KernelAnalyzer` runs one time.

## Build output

An app that `umpire new app` makes writes all build output to `artifacts~/` in the block folder. Its `Directory.Build.props` sets `UseArtifactsOutput` and `ArtifactsPath`. Unity skips each folder whose name ends with `~`, so it does not import the DLLs of the build a second time.

| Need | Do this in `Directory.Build.props` |
|------|-------------------------------------|
| Another output folder | Set `ArtifactsPath` to that folder |
| Output next to each project (`bin/`, `obj/`) | Set `UseArtifactsOutput` to `false` |

The .NET SDK reads these two properties only from `Directory.Build.props` or the command line. A value in a project file gives `NETSDK1199`.

## Layers that Unity can use

| Layer | Package | Unity |
|---|---|---|
| Domain | `Umpire.Vsa.Domain` | Yes |
| Contracts (`IMessage`, `Envelope`, `Result`) | `Umpire.Messages.Contracts` | Yes |
| Handler pipeline | `Umpire.Messages.Handlers` | Yes |
| Contracts, commands and events | `Umpire.Vsa.Contracts.Commands`, `Umpire.Vsa.Contracts.Events` | Yes |
| Handlers, commands and events | `Umpire.Vsa.Handlers.Commands`, `Umpire.Vsa.Handlers.Events` | Yes |
| Infrastructure | `Umpire.Vsa.Infrastructure` | Yes |
| Kernel (`App`, `Pack`, `AppHost`, `Module`) | `Umpire.Kernel` | Yes |
| Composition | `Umpire.Vsa.Composition` | Yes, without presenters. `VsaHost` and `Plugin` are here |
| Contracts, requests | `Umpire.Vsa.Contracts.Requests` | Yes. The route members are only on `net10.0` |
| Handlers, requests | `Umpire.Vsa.Handlers.Requests` | Yes |
| Presenters | `Umpire.Vsa.Presenters` | Yes |
| Architecture definition | `Umpire.Architecture`, `Umpire.Vsa` | Yes. `Pack` needs them, and `Vsa<THost>` is here |
| Host; Views | ASP.NET Core packages | No: ASP.NET Core |

## Rules for the netstandard2.1 target

- `vendor.cs` does not copy a DLL that Unity ships. A second copy is a compile error.
- `netstandard2.1` does not have `IsExternalInit`. The layers compile `tools/sdk/Polyfills/` for that target. The polyfill is internal, so each Unity assembly with a record has its own copy.
- `AppTypes` finds the modules in the loaded assemblies, so a Unity app gives no modules. A Unity app calls `new MyApp().Run(args)` through the Unity host, or `new Vsa<UnityVsaHost>().CreateHost().Compose(services, app)` with no host run (`docs/composition.md`). It vendors `Microsoft.Extensions.Hosting` 8.0 and its dependencies, because Umpire.Vsa.Composition references them.
- In Unity, an assembly definition (`.asmdef`) is one layer. Its `precompiledReferences` give the VSA DLLs that the layer can use.

## Non-goals

- No IL2CPP, stripping, `link.xml` or Unity reflection design in this repository. Reason: `vsa-unity` owns the Unity build, and it can test these limits in a Unity player.

## Open questions

- Unity CI: a Unity license in GitHub Actions needs a secret of the license. The Unity check runs on a dev machine only.
- Unity package: a UPM package can replace the vendored DLLs.
