# Vocabulary

Each term has one meaning in the code, the docs and the messages. Umpire is the product, and VSA is one architecture pack of Umpire.

To make a first app and fix a first rule error, see [getting-started.md](getting-started.md).

## Product and packs

| Term | Meaning | Example |
|---|---|---|
| Umpire | The product: framework, rule engine, tool and SDK | `Umpire.Kernel`, `umpire` tool |
| Pack | One architecture with its definition, bases and rules | `packs/Vsa/`, `Vsa<THost>` |
| Pack variant | A pack whose architecture inherits the architecture of another pack and adds parts and rules | `Packs/Ecs/`, `EcsArchitecture : VsaArchitecture` |
| VSA | The Vertical Slice Architecture pack | `Umpire.Vsa.*`, `VsaArchitecture` |
| Architecture | A definition: concepts, parts and rules | `VsaArchitecture : Architecture` |
| Host | The startup of one app kind | `WebVsaHost`, `ConsoleVsaHost` |

## Block kinds

| Kind | Meaning | Example |
|---|---|---|
| App | A block that runs. Its app class is the check entry and the install point | `NotesApp`, `EngineApp` |
| Pack | A block with one architecture and its kernel | `packs/Vsa/` |
| Framework kernel | Free base code that each app has, with no layer. It owns the host | `framework/Kernel/`, `AppHost` |
| Pack kernel | The layer bases of a pack. It extends the framework kernel | `packs/Vsa/Kernel/`, `VsaHost` |
| Component | Free code that a kernel installs by name | `framework/Components/Messages/` |
| Module | A component that the host finds in the app. It ships with the app | A feature module, a plugin module |
| Library | A block with no install point | `engine/Versions/` |

A component can have a layer shape. That does not make it a module. A plugin is a VSA module kind: the base of the same layers in each module. See `blocks.md`.

## Definition terms

| Term | Meaning |
|---|---|
| Block | A folder tree of code with one entry. The top part of a definition (`Define(Block block)`) |
| Independent block | A block with its own definition and check entry, for example an app or a pack |
| Owned block | A block that gets its shape from the definition around it, for example a feature module |
| Install point | The class or method that tells the host what to install from a block, for example `Pack.CreateHost()` |
| Block folder | The folder where a definition applies: the folder of the nearest check entry, or `--block` (`UmpireBlock`) |
| Check entry | The pointer from a block to its architecture: an app class, a `Block<TArchitecture>` subclass, the `AssemblyMetadata` `Umpire.Architecture`, or an `umpire.json` file. Each block has one |
| Block place | A folder that an outer definition marks with `AddBlock<T>()` or `AddBlockGroup<T>()` as the place of another block. The outer definition sees that block as one part |
| Concept | A kind of part that a definition names, for example a layer (`record Domain : Layer`) |
| Part | One place in the folder tree of a definition |
| Module (definition) | The record `Module`: the root part of a group instance, for example a feature folder. The block kind is in "Block kinds" |
| Module kind | A group of modules with one shape, for example `Feature`, `Plugin` or the `EcsFeature` of a pack variant |
| Define method | A `virtual` method of `VsaArchitecture.Define` for one base group, for example `DefineFeature(PartGroup feature)` |
| External | A part that is a DLL, not a folder, for example `Kernel` |
| Rule | Who may use whom (`rules.For<A>().Uses<B>()`) |
| Queries | The answers of the rules on plain facts: `PartOf` (which part), `MayUse` (allow or deny), `Counts` and `Matches` (`architecture/Queries/`) |
| Count | A rule statement that counts the types of a concept in each instance of a group, or the types that take each type of a part (`Has<T>()`) |
| Match | A rule statement that a type takes a type argument of a concept, in the same instance of a group (`Takes<T>().InSame<G>()`) |
| Hook | A Before or After step of one message kind. Its parts are `Handlers.Commands.Hooks` and `Handlers.Events.Hooks`, and a hook is in its own slice |

## Engine terms

| Term | Meaning |
|---|---|
| Step | One feature module of the engine with one command (`EngineStep<TResult>`) |
| Finding | One result line: code, severity, message, file and line |
| Baseline | The errors that `umpire-baseline.txt` accepts |
| Rule ID | `UMP` and six digits: area (2), group (2), case (2). Area `01` analyzers, `02` rule findings, `03` engine run |
