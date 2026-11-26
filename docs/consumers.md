# Release and consumers

Each push to `main` publishes all Umpire packages to GitHub Packages as the next preview. A tag `v<version>` publishes a stable version. Two public consumer repos use the published packages: a .NET app and a Unity project. They are the release check: the test apps of this repository build from source, and the consumers use only what ships.

To make a first app and fix a first rule error, see [getting-started.md](getting-started.md).

## Release flow: a merge reaches the consumers

```mermaid
flowchart LR
    M[Merge to main] --> W[Publish workflow]
    T[Tag v version] --> W
    W --> F[GitHub Packages feed]
    F --> D[vsa-consumer-dotnet]
    W --> R[GitHub release archive]
    R --> U[vsa-unity]
```

| Item | Value |
|---|---|
| Workflow | `.github/workflows/publish.yml` |
| Steps | Test, get the version, pack each project under `framework/`, push, make the release archive, attach it to the GitHub release |
| Version on `main` | `<base>-preview.<n>`: `<base>` is `<Version>` in `Directory.Build.props`, `<n>` is the highest preview tag plus one |
| Version on a tag | The tag name without `v` |
| Tag on `main` | The workflow pushes `v<base>-preview.<n>` before the packages. A rerun of the same commit uses that tag again |
| Feed | `https://nuget.pkg.github.com/MgCohen/index.json` |
| Package visibility | Public, the same as this repository |

A preview needs no manual step: merge the change to `main`.

To release a stable version:

1. Tag a commit on `main`: `git tag -a v0.2.0 -m v0.2.0`.
2. Push the tag: `git push origin v0.2.0`.
3. Set `<Version>` in `Directory.Build.props` to the next version, for example `0.3.0`. If you do not, the next previews (`0.2.0-preview.<n>`) sort below `0.2.0`, and consumers do not get them.

After a new version, update the Umpire version in each consumer repo, then make sure that its checks pass.

## Release archive

Each release tag `v<version>` has a GitHub release with one archive, `umpire-<version>.zip`. It is for a host that cannot restore NuGet packages, for example vsa-unity. A version with a suffix (`-preview.<n>`) is a prerelease. The repository is public, so a download needs no token. `tools/archive/archive.cs` makes the archive from the packed feed, so each file has the bytes of the NuGet packages.

```text
umpire-<version>.zip
├── manifest.json            version, commit, Roslyn version, engine runtime, one entry for each file
├── THIRD-PARTY-NOTICES.txt  the license of each third-party package, and the notices of the analyzer package
├── runtime/netstandard2.0/  Umpire.dll and its full third-party closure
├── definition/netstandard2.0/  Umpire.Definition.dll, Umpire.Vsa.Definition.dll (check-time code, not for the runtime)
├── analyzers/dotnet/cs/     Umpire.Kernels.Analyzers.dll, Umpire.Vsa.Analyzers.dll
└── engine/net8.0/           umpire-engine.dll, its runtimeconfig, its deps.json and its DLLs
```

| Manifest key | Value |
|---|---|
| `version` | The exact package version, for example `0.2.0-preview.21` |
| `commit` | The commit of the release (`--commit`, else the local `HEAD`) |
| `analyzers.roslyn` | The highest Roslyn version that the analyzers reference, for example `4.10` |
| `engine.targetFramework`, `engine.rollForward` | `net8.0`, `Major` |
| `engine.umpireBuild` | `netstandard2.0`: the build of `Umpire.dll` that the engine uses. Code that compiles against `runtime/netstandard2.0` has the same shape |
| `engine.frameworks` | Each shared framework that the engine needs, with its `name` and `version` |
| `files[].path` | The path in the archive |
| `files[].assemblyVersion` | The assembly version of a DLL. `null` for a file that is not a DLL |
| `files[].package`, `files[].packageVersion` | The NuGet package that gives the bytes. The analyzer files are `Umpire.Vsa.Analyzers`, and the Umpire engine files are `Umpire.Sdk` |
| `files[].asset` | The path of the file in that package, for example `lib/netstandard2.0/Microsoft.Extensions.Logging.dll` |
| `files[].sha256` | The SHA-256 hash of the file |

- The folder names and the file names stay the same in each release.
- The runtime holds no definition code. A definition of the host compiles against `runtime/netstandard2.0` and `definition/netstandard2.0`.
- The runtime holds all its DLLs, also the DLLs that a host ships itself. The host skips its own DLLs: the archive has no host list.
- The archive has no host files, for example no Unity `.meta` files.
- The runtime files come only from `netstandard2.x` assets, and the `Microsoft.Extensions` files are 8.0 (`tests/Release.Tests`).
- The script restores with the SDK of the `global.json` of this repository.

While CI is off, make the release on a local computer, after the pack:

1. Make the archive: `dotnet run tools/archive/archive.cs -- --feed feed --version <version> --out release --commit $(git rev-list -n1 v<version>)`.
2. Make the release: `gh release create v<version> release/umpire-<version>.zip --verify-tag --title v<version> --notes "Umpire <version>"`.
3. For a preview, add `--prerelease` to step 2.

`tests/Release.Tests` makes an archive from the local feed and checks its layout and its manifest.

## Feed access

GitHub Packages needs a token to restore, also for public packages.

| Where | Token |
|---|---|
| GitHub Actions | `GITHUB_TOKEN` of the workflow, with `packages: read` |
| Dev machine | `gh auth token`, after `gh auth refresh -s read:packages` |

The `nuget.config` that `umpire new app` writes reads the token from the environment variable `GITHUB_TOKEN`:

```xml
<add key="ClearTextPassword" value="%GITHUB_TOKEN%" />
```

If `GITHUB_TOKEN` is not set, restore fails with 401.

## An existing app

`umpire init` adds the Umpire setup to an app that exists. Run it in the folder of the root project:

```bash
dotnet tool exec Umpire.Cli@<version> -y -- init --check true   # show the missing setup, write nothing
dotnet tool exec Umpire.Cli@<version> -y -- init                # add it
```

It adds the missing parts of `nuget.config` and `.gitignore`, and it checks that a `global.json` does not pin `Umpire.Sdk`. It makes the Umpire SDK the main SDK of the root project, with the version (`Umpire.Sdk.Web` for a web project), and it writes `.umpireconfig` if the app has more than one project. It shows each step that it cannot do, for example with central package versions. Then write the app class (`UMP030502`) and run `dotnet build`. See "Setup pipeline" in [scaffolding.md](scaffolding.md).

## The architecture check of a new app

The Umpire SDK package holds the rule engine, so an app has no engine package. After each build of the root project, the SDK runs the engine check, and each finding is a build warning. `umpire.check.mode = "Error"` in `.umpireconfig` makes each error finding a build error, and `-p:UmpireCheckMode=Error` does it for one build. `TreatWarningsAsErrors` does not change an Umpire warning. The mode does not change the analyzer findings: `umpire.<ID>.severity = "error"` makes one an error. `-p:UmpireCheck=false` stops the check for one build. The check also covers each block that the app uses: each inner block (for example `App/Packs/<Name>/`) and each block with a check entry in a project reference. A block in a package is not checked again. Each block has a check stamp in `artifacts~/umpire-checks/`, so a block that did not change is not checked again. A manual engine run builds the root project, then checks it, and gives one report with the build findings and the architecture findings. The engine has the version of the SDK package, so `umpire doctor` and `umpire update` need no other slot.

## Packages of an app

`<Project Sdk="Umpire.Sdk" />` gives the package `Umpire`, with its own version: one `Umpire.dll` with the kernel and the VSA kernel with each layer base. `Umpire.dll` holds no definition code. `<Project Sdk="Umpire.Sdk.Web" />` also gives `Umpire.Web` (the web host and the views), and a test project gets `Umpire.Testing`.

| Package | Holds | Who gets it |
|---|---|---|
| `Umpire` | The runtime: the markers, the config reader, the kernel, the messages, the VSA kernel | Each project |
| `Umpire.Definition` | The definition API (`Umpire.Architecture`) and the definitions of the framework blocks (`Umpire.Kernels.Definition`) | A definition project (a project in a folder `Definition/`), and a test project through `Umpire.Testing` |
| `Umpire.Vsa.Definition` | The VSA definition (`VsaArchitecture`, the layers, `Feature`, `Plugin`) | A definition project with the default pack, and a test project through `Umpire.Testing` |
| `Umpire.Testing` | The test bases `VsaUnitTest` and `VsaArchitectureTest` | A test project (`*.Tests`) |

An app project does not compile against the definition packages. The architecture check still needs the definition of the pack, so the root project copies `Umpire.Definition.dll` and `Umpire.Vsa.Definition.dll` from the engine folder of the SDK package (`tools/net8.0/`, the same build) to its folder `umpire-definitions/`. A publish does not copy that folder. A project with definition code outside a folder `Definition/` adds the package itself, for example `<PackageReference Include="Umpire.Vsa.Definition" Version="<version of Umpire.Sdk>" PrivateAssets="all" />`, so the package does not flow to the root project. Or it sets the property `UmpireDefinitionProject` to `true`. A console app adds `Umpire.Console`. The code selects the pack: the app class names its kernel (`App<VsaKernel, ...>`). A package only makes a pack available. Another pack is a normal `PackageReference`, and `-p:UmpireDefaultPack=false` (or the property in the project) leaves out the VSA test bases and views. `Umpire` depends on `Umpire.Vsa.Analyzers`, so the analyzers and their rule values flow to each project that gets `Umpire`, also through another project. The analyzers of a pack run only in a compilation that selects that pack: an app class of the pack, a `Block<T>` or an `AssemblyMetadata` entry that names its architecture, the `umpire.architecture` key, or, with no entry, a reference to a type in the namespace of the pack. `Umpire.dll` always holds the namespace `Umpire.Vsa`, so a project with `UmpireDefaultPack=false`, another pack and no code entry still gets the VSA analyzers. Give such a project a code entry or the `umpire.architecture` key. `Umpire.Vsa.Analyzers` holds the VSA analyzer and the kernel analyzer in one folder, with the rule values of both (pinned choice, `framework/Kernel/Analyzers/Umpire.KernelBundle.targets`). To change a rule value or a severity, set the key in the `.umpireconfig` file of the block.

There are two Umpire SDKs, one for each platform SDK of .NET:

| SDK | Platform SDK | Root project |
|---|---|---|
| `Umpire.Sdk` | `Microsoft.NET.Sdk` | `<Project Sdk="Umpire.Sdk/0.2.0" />` |
| `Umpire.Sdk.Web` | `Microsoft.NET.Sdk.Web` | `<Project Sdk="Umpire.Sdk.Web/0.2.0" />` |

An Umpire SDK must be the main SDK of the project, because it sets the output folder before the platform SDK does. It also does these things:

- **Block root.** The block root is the folder of the nearest `.umpireconfig` file at or above the project. With no `.umpireconfig` file, it is the project folder. A block with more than one project needs a `.umpireconfig` file, also an empty one.
- **Output.** Each project writes to one `artifacts~` folder in the block root, in the .NET artifacts layout (`artifacts~/bin/<Project>/`, `artifacts~/obj/<Project>/`). No `bin/` or `obj/` folder is next to the code. A `Directory.Build.props` file that sets `ArtifactsPath` wins.
- **Rule values.** `.umpireconfig` is TOML, and it holds only Umpire values (`umpire.<ID>.<key>`, `umpire.<ID>.severity`). The SDK gives the file to the compiler as an additional file, and the Umpire analyzers read it with Umpire.Config. A value there wins over the default on the rule class. A file-pattern section, for example `[*.cs]`, is not valid TOML: the build gives `UMP010002`, and the engine run stops with the line of the error. The severities of the .NET rules stay in the `.editorconfig` of the app.
- **Module settings.** The SDK copies `.umpireconfig` to the output folder of each project. A module reads the table with its name (`docs/composition.md`).
- **Rule engine.** The root project of the app (an executable that is not a test project) runs the engine of the SDK package after its build. `-p:UmpireEngine=false` turns it off.
- **Test project.** A project whose name ends with `.Tests` gets NUnit on Microsoft.Testing.Platform, `using NUnit.Framework` and the test bases of the pack. It references the projects that it tests itself. To make such a project a normal project, set `UmpireTestProject=false` in a `Directory.Build.props` file: the project body is too late, because the platform SDK reads `OutputType` first. A `*.Tests` project never gets the rule engine.

Umpire.Sdk ships no native .NET style. The app sets its own style in its `.editorconfig`.

## Consumer repos

| Repo | Contains | Gets VSA | Check |
|---|---|---|---|
| [vsa-consumer-dotnet](https://github.com/MgCohen/vsa-consumer-dotnet) | `umpire new app`, one module | `<Project Sdk="Umpire.Sdk" />` | CI: build and test |
| [vsa-consumer-unity](https://github.com/MgCohen/vsa-consumer-unity) | Unity 6 project, one module | DLLs in `Assets/Plugins/VSA` | Unity Test Framework, batch mode |

## Unity

The Unity consumer gets the DLLs, not the packages. The Unity limits, their tests and the extension points are in `docs/unity.md`.
