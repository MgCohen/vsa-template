# Blocks: kinds, entries and composition

A block is a folder tree of code with one entry. This page gives the kinds of block, their entries and the composition guidelines. The engine checks of the blocks are in "Blocks and check entries" in `architecture-engine.md`.

Each guideline has one of two marks:

| Mark | Meaning |
|---|---|
| **Checked** | The engine or the host checks it. The code ID or the check is in the row |
| **Not enforced in 0.1.0** | A guideline. No analyzer, rule or engine step checks it |

## The block tree

```
Block                    core meaning                                   example
 ├─ App                  the code root of an app                        App/ (MyApp : App<TKernel>)
 ├─ Definition           the root of the architecture class             Definition/
 ├─ Pack                 an architecture with its kernel                packs/Vsa (VsaArchitecture, VsaKernel)
 ├─ Kernel               installs an architecture; owns the hosts       Core (framework/Kernel), the kernel root (packs/Vsa/Kernel)
 ├─ Component            code that travels together, no entry point     MessagesComponent (framework/Components/Messages), engine/Versions/
 │   └─ Module           a component that the host installs             Feature, Plugin
 └─ Slice                an owned block of a feature                    App/Features/<Feature>/Slices/<Slice>
```

| Namespace | Records |
|---|---|
| `Umpire.Architecture.Blocks` | `Block`, `App`, `Definition`, `Kernel`, `Component` |
| `Umpire.Kernels.Blocks` (`framework/Kernel/Definition/`) | `Core`, `Module`, `Pack` |
| `Umpire.Kernels` (`framework/Kernel/Definition/`) | `MessagesComponent` |
| `Umpire.Vsa.Definition` | `Feature`, `Plugin`, `Slice` |

`App` and `Definition` are roots: the base `Architecture.Configure` adds them with `config.Roots.Add<T>()`. The kernel root is a part of the concept `Kernel` with the folder `Kernel`. `Define(Part root, Part kernel)` writes its body into `kernel`.

- An **independent block** has its own definition and its own check entry, for example an app or a pack.
- An **owned block** gets its shape from the definition of the block around it, for example each feature module of an app (`AddGroup<Feature>()`).
- A block is not a project. A project of types that only one block uses is a part of that block, for example `architecture/Markers/` or the fixture `Kit`.
- A module is consumer code and ships with its app. A component is free code that a kernel installs by name. A module is found by the host. That is the difference.
- There are two kernels. The core (the record `Core`, `framework/Kernel/`) has no layer, and it owns the platform hosts (`Hosts/`). `Architecture<TKernel>` adds it as a root. The kernel root of a pack (`packs/Vsa/Kernel/`) adds the layers of its architecture. `VsaArchitecture` writes the layers into the kernel root and places `MessagesComponent`.
- A block has three forms: source (folders), a DLL with structure (bound by the PDB path) and an opaque DLL (`External`, bound by name).

## Two entries

A block can have two entries. They answer two different questions.

| Entry | Question | Which blocks |
|---|---|---|
| Check entry | Which architecture checks this block? | Only an independent block |
| Install point | What does the host install from this block? | Each kind, except a library |

For an app, the app class is the check entry: a closed class that implements `IAppEntry<TKernel>`, for example `MyApp : App<TKernel>` or a Unity shell. It names the kernel, and the engine finds the definition (see "Find the definition" in `architecture-engine.md`). Its install point is the custom host, if the app has one.

## Entries of each kind

| Kind | Check entry | Install point | Mark |
|---|---|---|---|
| App (the root `App`) | The app class | None on the app class. A custom host `Configure` (`MyHost : WebHost`) | Checked (`UMP030502`, `UMP030503`) |
| Pack | The definition in `Definition/`, for example `VsaArchitecture : Architecture<VsaKernel>` | None. The app class names the kernel of the pack | Checked (`PackArchitecture`) |
| Core | `AssemblyMetadata` | `Core.Install` (the class `Umpire.Kernels.Core`) | Checked: folders only (`KernelArchitecture`) |
| Kernel root of a pack | None: the `kernel` part of `Define`. `PackArchitecture` gets the kernel body of the packed architecture | `VsaKernel.Configure` | Checked with the pack: the kernel body and rules |
| Component | `AssemblyMetadata`, optional | An `Add<Name>` extension, or none | Checked: folders only (`MessagesArchitecture`, `ComponentArchitecture`, `ApiArchitecture`) |
| Module | None: the app owns it | A `Module` class at the top of the module folder. Each module kind derives from it, for example `Plugin` | Checked (`UMP020301`, host startup) |

The check entry has five forms. The kinds above give the default form. Each form is possible for each independent block.

| Form | Example |
|---|---|
| App class | `NotesApp : App<VsaKernel>` |
| Definition of a kernel | `VsaArchitecture` in `packs/Vsa/Definition/` |
| Typed base | `public class ToolsBlock : Block<ToolsArchitecture>`. The project needs the definition package (see "Packages of an app" in `consumers.md`) |
| `AssemblyMetadata` | `<AssemblyMetadata Include="Umpire.Architecture" Value="<type>, <assembly>" />` |
| `.umpireconfig` | `umpire.architecture = "<type>, <assembly>"` |

- Each independent block has exactly one check entry. **Checked** (`UMP030301`).
- A check entry names a type that loads and that is an `Architecture`. **Checked** (`UMP030302`).
- An entry in code wins over the key `umpire.architecture` in the same folder. **Checked** (`UMP030304`, a warning).
- A typed base counts only for a type that is not abstract. **Checked** (`FindBlocks`).
- The framework definitions (`KernelArchitecture`, `MessagesArchitecture`, `ApiArchitecture`, `ComponentArchitecture`, in `framework/Kernel/Definition/`) have the folders of the block as parts, no rules and `DefaultPermission = Allow`. `KernelArchitecture` and `MessagesArchitecture` place the records `Core` and `MessagesComponent` with `InParentFolder()`, so each body is written one time. The same pipeline checks them as each other definition. A run of an app finds them as used blocks.

A component installs with a public extension that the kernel calls by name. `VsaKernel.Configure` calls each handler kind, and each kind calls `AddPipeline` of the bus of the kernel:

```csharp
protected override void Configure(IServiceCollection services, AppTypes types) {
    services.AddRequestHandlers(types);
    services.AddCommandHandlers(types);
}
```

## Shapes of the core kinds

| Kind | Folder shape | Mark |
|---|---|---|
| Pack | `Definition/` (the architecture of the kernel), `Kernel/`, `Tests/` (the tests of the definition, on `ArchitectureTest<TArchitecture>`, see `architecture-engine.md`) | Checked (`PackArchitecture`) |
| Core | Free: one folder for each topic | Not enforced in 0.1.0 |
| Component | Free | Not enforced in 0.1.0 |
| Kernel root of a pack | The folder `Kernel/` of the pack, with the kernel body of the packed architecture | Checked: the kernel body and the kernel rules |
| App, module | The shapes of `VsaArchitecture` | Checked (`UMP0201xx`-`UMP0203xx`) |

The core and each component have one install point. **Not enforced in 0.1.0.**

## Composition

| Guideline | Mark |
|---|---|
| A source file belongs to the nearest block folder above it | Checked (`FindBlocks`) |
| `AddBlock<T>()` marks a place, and the place gets the body of the record `T` | Checked (binding) |
| The outer definition adds no part to a block place directly. An overlay `For<T>().Add<TPart>()` adds a part to each placed part of concept `T` | Checked (`Define` error) |
| A block that places itself, directly or through another block, is an error | Checked (`Define` error) |
| A part that covers the folder of an inner block is an error | Checked (`UMP020401`) |
| A place folder with no check entry is an error. A place with no folder is the DLL form | Checked (`UMP020402`) |
| A type of a DLL that an `External` names binds to the body of its placed block by the PDB path. It is a target only | Checked (binding) |
| The block of the type that makes a use decides that use | Checked (binding) |
| A module with a different style is a new module kind of a pack variant | Not enforced in 0.1.0 |
| A module is source in its app | Not enforced in 0.1.0 |

The outer block writes rules only about its own uses of an inner block:

```csharp
var tools = root.AddBlock<Tools>();
rules.For<Module>(Depth.Self).Uses<Tools>();
```

`rules.For<Tools>().Uses<Module>()` is a use that the inner code makes, so only the inner block writes it.

A module with a different style (for example ECS) is a module kind next to `Feature`, in a pack variant that inherits `VsaArchitecture`. The app definition still checks each module. See `pack-variants.md`. Use `AddBlock<T>()` only for a really separate block: then it is not a module of the app, and the app sees it as one part.

The framework blocks come from the definitions. In an app, each one is a DLL. `Architecture<TKernel>` adds the root `Core`, and `VsaArchitecture` writes the kernel body and places `MessagesComponent`:

```csharp
protected override void Define(Part root, Part kernel) {
    kernel.Add<Domain>().And<Infrastructure>().And<Presenters>();
    root.AddBlock<MessagesComponent>();
}
```

In a DLL that an `External` names, the engine reads the source path of each type from the PDB. It finds the folder with the name of the record (`Folder`) whose next folder is a part of the body:

```
/_/packs/Vsa/Kernel/Domain/Entity.cs        -> Kernel/Domain
/_/framework/Kernel/Hosts/Web/WebHost.cs    -> Core/Hosts
/_/framework/Components/Messages/Contracts/Envelope.cs -> MessagesComponent/MessagesParts.Contracts
/_/framework/Kernel/Messages/Pipeline/Pipelines.cs -> Core/KernelParts.Messages
```

If two placed blocks match one path, the engine gives an error that names both. Give one record a longer `Folder`, for example `"Vsa/Kernel"`. The bundle packages embed their PDB with the source paths under `/_/` (`tools/bundle/Directory.Build.props`).

## Non-goals

- No check of the uses that the code of an inner block makes, in the run of the outer block. Reason: the inner block has its own check, and each use has one owner. In the outer run, the types of an inner block or of a DLL are targets only.
- No `virtual` property as a check entry. Reason: the engine reads the entries with no instance.
- No check entry by a name convention. Reason: an entry is always a type or a written name, so a reader sees it.

## Open questions

- `Block<T>` as a typed check entry for module kinds, and the block name family (#202).
- The runtime `Kernel` class: the pack can install the layers itself (q29 of the decision log).
- The block root in a DLL by folder name and body: review it before the first preview (d47).
- A guideline shape for the core.
- A component that brings its own piece of definition (spike first).
- A shared module as an independent block in its own repository, seen by the app as an external part.
- The names `Component` and the kernel records (names review).
