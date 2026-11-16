# Vocabulary

Each term has one meaning in the code, the docs and the messages. Umpire is the product, and VSA is one architecture pack of Umpire.

## Product and packs

| Term | Meaning | Example |
|---|---|---|
| Umpire | The product: framework, rule engine, tool and SDK | `Umpire.Kernel`, `umpire` tool |
| Pack | One architecture with its definition, bases and rules | `packs/Vsa/`, `Vsa<THost>` |
| VSA | The Vertical Slice Architecture pack | `Umpire.Vsa.*`, `VsaArchitecture` |
| Architecture | A definition: concepts, parts and rules | `VsaArchitecture : Architecture` |
| Host | The startup of one app kind | `WebVsaHost`, `ConsoleVsaHost` |

## Tiers of code

| Tier | Meaning | Checked by |
|---|---|---|
| Kernel | Free base code that each app has, with no layer | No layer rules |
| Component | A free block that many definitions can use, for example `Messages` | No layer rules |
| Plugin | A VSA module that is the base of the same layers in each module | The VSA definition |

A component can have a layer shape. That does not make it a plugin. "Component" is only a free block, and "Plugin" is only the checked VSA module.

## Definition terms

| Term | Meaning |
|---|---|
| Concept | A kind of part that a definition names, for example a layer (`record Domain : Layer`) |
| Part | One place in the folder tree of a definition |
| Module | The root part of a group instance, for example a feature folder |
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
| Rule ID | `UMP` and six digits: block (2), group (2), case (2). Block `01` analyzers, `02` rule findings, `03` engine run |
