# Open questions for the user

## K4: config, DLLs, packages (#331, #247)

The user has this list and did not answer it. An opinion is not a decision.

### Answers

- **Q1 (2026-11-25).** "No definition code on umpire DLL. The definition code will most likely need the umpire DLL, so the helpers can stay there, or we can move them into a utility." Rule of the user: instead of one using the other, move shared things to a utility.
- **Q2 and Q6 (2026-11-25).** "We can keep splitting": `Umpire` (the core), `Umpire.Definition` (the core definition), `Umpire.Vsa` (the host/kernel), `Umpire.Vsa.Definition` (the VSA definition). This also answers Q6: the core and VSA are two DLLs.
- **Q3 (2026-11-25).** Not answered. "We still have to talk about the topology and architecture and split of this, because I don't like the idea of check model. ... But it can live wherever it needs to live." New open item: the structure of the check code, before its place.
- **Q4 (2026-11-25).** "Our own reader, even if inside our DLL it uses Tomlyn or whatever we define. No one needs to know that it's behind our own interface, our own utility. But we can try to go with our own small one. Let's see how that plays out."

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
