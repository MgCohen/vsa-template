# Hand-over: review and unify the check system

Read this file first in a new session. It gives the state, the decisions, the next work and the working rules. It is a work file: change it when the state changes, and delete it when the phase ends.

| File | Content |
|---|---|
| `README.md` | State, decisions, queue, working rules |
| `next-work.md` | The next code work (brief D) |
| `open-questions.md` | Questions that wait for the user: K4, then the parked ones |

## Goal of the phase

Make one base check system. VSA details come later.

| Key decision | State |
|---|---|
| K0 engine, K1 reader, K2 jobs and rules | Decided |
| K3 run model | Agreed for now (#293) |
| K4 config, DLLs, packages (#331, #247) | Questions are ready, not answered |
| K5 definition API (#344) | Not started |
| K6 | After K5, see the issues with the label `blocked: K1-K6` |

## State of the code

- Streams A (check model), B (link reversed) and C (VSA rule lines) are in `main` (#375).
- Exit codes, the warning default and `UMP030513` are in `main` (#377). This repository stays strict (root `Directory.Build.props`, `engine/.umpireconfig`).
- The check model is in `check/` (`docs/check-model.md`). The build still uses the engine in `engine/`. The switch to the new check is not done.
- Last published preview: `0.2.0-preview.24`. It does not have #375 and #377. The consumers are not updated.
- #78 is a paused draft. Do not touch it.

## Decisions

**K0.** The engine is the core system that holds many tools. It governs them and gives them one shape. It can run limited streams, and each trigger runs only the tools that it needs.

**K1.** Our own solution replaces ArchUnitNET.

**K2 (Q2 to Q5)**
- Five jobs: format, code check, architecture check, setup check, unit testing. A rule is a line in the definition, and many tools read it.
- A check reads one kind of rule line for any concept. No pack has its own check code (#143).
- No forced `.globalconfig`. The architecture config gets a placement option, by folder or by namespace (#351). The default is not decided.
- One tool reads `.umpireconfig`. The analyzers get the values as a product of the engine (#284).
- One Finding shape inside the engine. Each output converts it.
- No baseline file now (#347).

**K3**

```
definition + one snapshot for each project -> app model (not saved) -> jobs -> one report for each job
```

- An operation gets the app model and the report. The report has succeeded, findings and metadata.
- The analyzer works type by type. Its own project comes from symbols. It saves the snapshot at the end of a build.
- Old or missing data gives a warning, never an error. The last job gives the sure error.
- Generic rules (comments, sizes, member order) are normal analyzers outside the engine.
- The definition is not an input of the compile. Another job writes it as JSON before the analyzer needs it.
- One snapshot for each target framework. A hash can select the current data.
- `nameof`, a constant and an enum value are a use.
- Dependency inversion or dependency injection separates the systems: the analyzer only needs the app model ready.

**First version.** Findings are warnings by default and do not block a build, also with `TreatWarningsAsErrors`. The command has at least three exit codes: all good, the run ended and has findings, something broke. A definition source that no build loads gives a warning.

**D1 to D3 (link reversed)**
- The runtime is `App<VsaKernel, WebHost>` with no definition. The definition is `Architecture<VsaKernel>` in its own check-time DLL.
- The runtime pack class is removed. A pack is only a concept: a block kind and folders (#198).
- `Architecture<TKernel>` is a work form. Examine it later.
- To find the architecture: the app, its kernel, then the architecture that matches the kernel closest. If two have the same kernel, the one in the `Definition/` of the app wins (technical choice).

**Moved to later by the user**
- VSA rule details (#143): records for plugins and interfaces, the three presenter findings, the kernel rules of `packs/Vsa/`.
- The meaning of "data" (#142). The user decided to remove `UMP010206`.
- Which request rules the hosts need in the core (#376).

## Queue

1. `next-work.md` (brief D).
2. K4 answers of the user, then the K4 work.
3. The switch of the build to the new check, and then the removal of the ArchUnitNET reader.
4. Managed files, then the depth of each operation (#362).
5. K5 (#344), K6, #359, #360.
6. A preview, then the consumer updates. These need the local computer (Unity), see `docs/consumers.md`. The app class changed, so the consumers must change too.

## Working rules of the user

**Replies and questions**
- Short replies, the answer first. Write all prose in ASD-STE100 Simplified Technical English.
- Ask in the chat as text, one question at a time or groups of three. Give the context, a snippet, the options and an opinion.
- Decide technical questions without the user. Ask only for design or policy.
- "Skip for now" means: move the item to the end of the queue.
- Keep each decision in the words of the user. Do not add technology choices.
- Reject an idea only if it breaks a Must of `docs/requirements.md`. Else rank it down, with its reason, its source and a "come back if" note.

**Merges and tests**
- The user decides each merge of a PR about rules, analyzers or the public API.
- CI is off. Run only the affected test projects on your computer, and the full suite (with `Consumer.Tests` and `tests/Cli`) before a large merge.
- Let one subagent that did not write the code review each diff.
- Publishing is free until 1.0. Breaking changes are permitted before 0.1.0.
- Comment each DLL or target framework change on #247.
- Open an issue only for research, bugs and future problems.

**Code**
- No tuples, no `global::`, no strings or regex in definitions, no comments in C#.
- A property has the name of its type. No empty plumbing classes in a consumer app.
- Unity concerns belong to the vsa-unity repository. Do not push to it from here.
- Never print a token.

## Cloud notes

- All tests ran on Windows until now. On Linux, some path and start tests can fail at first. Record them before other work.
- The repository needs the .NET SDK of `global.json`. The tests that need .NET 8 skip without it.
- Unity, Visual Studio and the consumer work stay on the local computer.

## Private pages (only the user can open them)

| Page | Link |
|---|---|
| Check goals and the K3 model | https://claude.ai/artifact/XA38aCcCmHVL8j97egQmEj |
| Plan | https://claude.ai/artifact/NACemopjJv2LcsEFoY22H6 |
| Tracker (its database has the rows) | https://claude.ai/artifact/6W18kfHNc1MTa5FA1Vi5AA |
