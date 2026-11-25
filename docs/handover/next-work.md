# Next work: the jobs of the check model (brief D)

Base: `main`. Make draft PRs, small and complete, with one reviewer each. Do not merge.

| # | Work | Done when |
|---|---|---|
| 1 | One snapshot for each target framework, selected by hash | Findings that are equal in two targets show one time. The snapshot of a removed target does not stay |
| 2 | The after-build job writes `definition.json`, `block.json` and the library files, also in package mode | A consumer build has all files with no manual step. The PR gives the time on Notes |
| 3 | The app model gets its data from small interfaces (definition source, snapshot source) | The model references no Roslyn, MSBuild or definition API. Files, symbols and tests are implementations |
| 4 | `nameof`, a constant and an enum value are a use | A test for each form, and no open question in `docs/check-model.md` |
| 5 | Remove `UMP010206` | Its ID is not used again. #142 stays open |
| 6 | A multi-target root does not need to clear `TargetFramework` | The workaround is not in the docs, and a package-mode test passes |

Limits: do not change the VSA rule lines. The engine in `engine/` stays the check of the build until the user decides the switch.

Measure only, do not change: the size of a snapshot (about 7 KB for each type).
