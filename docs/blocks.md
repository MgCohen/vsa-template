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
 ├─ App                  it runs; the app class is both entries         tests/Fixtures/Notes, engine/
 ├─ Pack                 an architecture with its kernel                packs/Vsa (Vsa<THost>)
 ├─ Kernel               installs an architecture; owns the hosts       framework/Kernel (Kernel, AppHost) -> packs/Vsa/Kernel (VsaKernel)
 ├─ Component            installed by name by the kernel that uses it   framework/Components/Messages (AddPipeline)
 │   └─ Module           a component that the host finds in the app     a VSA feature (Module), a VSA plugin (Plugin)
 └─ Library              no install point                               engine/Versions/, tools/Files/
```

- An **independent block** has its own definition and its own check entry, for example an app or a pack.
- An **owned block** gets its shape from the definition of the block around it, for example each feature module of an app (`AddGroup<Feature>()`).
- A block is not a project. A project of types that only one block uses is a part of that block, for example `architecture/Markers/` or the fixture `Kit`.
- A module is consumer code and ships with its app. A component is free code that a kernel installs by name. A module is found by the host. That is the difference.
- There are two kernels. The framework kernel (`framework/Kernel/`, `Kernel`, `AppHost`) has no layer, and it owns the platform hosts (`Hosts/`). A pack kernel (`packs/Vsa/Kernel/`, `VsaKernel`) extends it with the layers of its architecture.

## Two entries

A block can have two entries. They answer two different questions.

| Entry | Question | Which blocks |
|---|---|---|
| Check entry | Which architecture checks this block? | Only an independent block |
| Install point | What does the host install from this block? | Each kind, except a library |

For an app, the app class is the check entry. Its install point is the custom host, if the app has one.

## Entries of each kind

| Kind | Check entry | Install point | Mark |
|---|---|---|---|
| App | The app class | None on the app class. A custom host `Configure` (`MyHost : WebHost`) | Checked (`UMP030502`, `UMP030503`) |
| Pack | `Pack : Block<PackArchitecture>` | None. The pack names its kernel and its host | Checked (`PackArchitecture`) |
| Framework kernel | `AssemblyMetadata` | `Kernel.Install` (the core) | Checked: folders only (`KernelArchitecture`) |
| Pack kernel | None: the architecture of the pack defines it (`DefineKernel`, `KernelRules`) | `VsaKernel.Configure` | Checked with the pack: folders only |
| Component | `AssemblyMetadata`, optional | An `Add<Name>` extension | Checked: folders only (`MessagesArchitecture`, `LibraryArchitecture`) |
| Module | None: the app owns it | A `Module` class. Each module kind derives from it, for example `Plugin` | Checked (`UMP020301`, host startup) |
| Library | `AssemblyMetadata`, optional | None | Checked: folders only (`LibraryArchitecture`, `ApiArchitecture`) |

The check entry has four forms. The kinds above give the default form. Each form is possible for each independent block.

| Form | Example |
|---|---|
| App class | `NotesApp : App<Vsa<WebHost>>` |
| Typed base | `public abstract class Pack : Block<PackArchitecture>` |
| `AssemblyMetadata` | `<AssemblyMetadata Include="Umpire.Architecture" Value="<type>, <assembly>" />` |
| `.umpireconfig` | `umpire.architecture = "<type>, <assembly>"` |

- Each independent block has exactly one check entry. **Checked** (`UMP030301`).
- A check entry names a type that loads and that is an `Architecture`. **Checked** (`UMP030302`).
- An entry in code wins over the key `umpire.architecture` in the same folder. **Checked** (`UMP030304`, a warning).
- A typed base counts only for a type that is not abstract. **Checked** (`FindBlocks`).
- The framework definitions (`KernelArchitecture`, `MessagesArchitecture`, `ApiArchitecture`, `LibraryArchitecture`, in `framework/Kernel/App/`) have the folders of the block as parts, no rules and `DefaultPermission = Allow`. The same pipeline checks them as each other definition. A run of an app finds them as used blocks.

A component installs with a public extension that the kernel calls by name. `VsaKernel.Configure` calls each handler kind, and each kind calls `AddPipeline` of the Messages component:

```csharp
protected override void Configure(IServiceCollection services, AppTypes types) {
    services.AddRequestHandlers(types);
    services.AddCommandHandlers(types);
}
```

## Shapes of the core kinds

| Kind | Folder shape | Mark |
|---|---|---|
| Pack | `<Name>Pack.cs`, `Architecture/`, `Kernel/`, `Tests/` (the tests of the definition, on `ArchitectureTest<TArchitecture>`, see `architecture-engine.md`) | Checked (`PackArchitecture`) |
| Framework kernel | Free: one folder for each topic | Not enforced in 0.1.0 |
| Component | Free | Not enforced in 0.1.0 |
| Library | Fully free | Not enforced in 0.1.0 |
| Pack kernel | The folder `Kernel/` of the pack, with the kernel parts of the pack's architecture | Checked: folders only, no rules yet |
| App, module | The shapes of `VsaArchitecture` | Checked (`UMP0201xx`-`UMP0203xx`) |

The framework kernel and each component have one install point. **Not enforced in 0.1.0.**

## Composition

| Guideline | Mark |
|---|---|
| A source file belongs to the nearest block folder above it | Checked (`FindBlocks`) |
| `AddBlock<T>()` and `AddBlockGroup<T>()` mark a place only | Checked (`UMP020401`, `UMP020402`) |
| The outer block uses an inner block as one part | Checked (`UMP020101`) |
| The block of the type that makes a use decides that use | Checked (binding) |
| A module with a different style is a new module kind of a pack variant | Not enforced in 0.1.0 |
| An architecture maps the DLLs of a component into its own parts | Not enforced in 0.1.0 |
| A module is source in its app | Not enforced in 0.1.0 |

The outer block writes rules only about its own uses of an inner block:

```csharp
var tools = block.AddBlock<Tools>();
rules.For<Kernel>().Uses<Tools>();
```

`rules.For<Tools>().Uses<Kernel>()` is a use that the inner code makes, so only the inner block writes it.

A module with a different style (for example ECS) is a module kind next to `Feature`, in a pack variant that inherits `VsaArchitecture`. The app definition still checks each module. See `pack-variants.md`. Use `AddBlock<T>()` only for a really separate block: then it is not a module of the app, and the app sees it as one part.

Each architecture gives a component its own meaning. It maps the DLLs of the component into its own parts:

```csharp
var contracts = kernel.Add<Contracts>().InAssembly("Umpire.Messages.Contracts");
```

## Non-goals

- No rules about the content of an inner block in the outer definition. Reason: the inner block owns its content, and each use has one owner.
- No `virtual` property as a check entry. Reason: the engine reads the entries with no instance, and it cannot make an open generic such as `Vsa<THost>`.
- No check entry by convention. Reason: an entry is always written, so a reader sees it.

## Open questions

- The `Component` base class, and `Module : Component` in code (composition review).
- `Block<T>` as a typed check entry for module kinds, and the block name family (#202).
- A tool as an app with a lighter host: `tools/cli` has no app class.
- Library: a kind of its own, or not. An optional entry utility for a library.
- A guideline shape for the framework kernel.
- A component that brings its own piece of definition (spike first).
- A shared module as an independent block in its own repository, seen by the app as an external part.
- The names "Component", "Library" and the two kernels (names review).
