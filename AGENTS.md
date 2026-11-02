# AGENTS.md

Instructions for AI coding agents (and humans) working in this repository. This file is model-agnostic: do not add tool-specific files such as `CLAUDE.md`; put shared guidance here.

## Project

A Vertical Slice Architecture (VSA) template: base infrastructure, scaffolding, a sample feature, and architecture rules, meant to be reused as the starting point for new projects.

## Layout

| Path                  | Purpose                                                                 |
|-----------------------|-------------------------------------------------------------------------|
| `framework/Layers/`   | The framework code: one project for each layer base. Ships as NuGet packages |
| `framework/Sdk/`      | VSA.Sdk: the build setup of each module project and the layer references |
| `framework/Testing/`  | The checks that ship: `Core/` (the layer map), `Analyzers/`, `Architecture/` (ArchUnitNET), `Unit/` (NUnit) |
| `samples/`            | Sample apps that use the framework as a consumer does. Does not ship   |
| `tests/`              | Tests of the framework itself, with fixtures. Does not ship            |
| `docs/`               | Architecture decisions, conventions, and guides                         |

A module project is under a `Modules/` folder. Its references come from VSA.Sdk, from the folder of the project. Do not add references to a module project by hand: the build fails with `VSA0001`. Inside this repository, the sample projects import VSA.Sdk from source (`samples/VSA.Sdk.Dev.props`). A consumer project is only `<Project Sdk="VSA.Sdk" />`.

The host finds the modules by itself: `Program.cs` calls `builder.AddVsa()`. To add a module, add its files under `Modules/`. Do not edit `Program.cs`. Each module root has exactly one class that inherits `Module` or `Module<TSettings>`, else startup fails.

## Core principles

- **Organize by feature, not by layer.** Each slice owns its request, handler, validation, and endpoint in one place.
- **Slices are independent.** A slice does not reach into another slice's internals; share only through explicit, minimal shared code.
- **Keep shared code small.** Promote code to a shared location only when two or more slices genuinely need it.
- **Rules are enforced by tests.** Architecture rules live in `tests/` and must pass.

## Add a check

The levels, rules and base classes are in `docs/architecture-rules.md`. Use a library first: the built-in .NET analyzers, Roslyn, ArchUnitNET or NUnit. Tests use NUnit, so they also run in Unity.

1. If a built-in .NET rule does the check, set its severity in `framework/Sdk/Sdk/VSA.globalconfig`.
2. If one file shows the problem, write an analyzer that inherits `VsaAnalyzer` in `framework/Testing/Analyzers/`. Use the next free `VSA1xxx` ID, and add it to `AnalyzerReleases.Unshipped.md`. Read options with `Options(...)`.
3. Else write an ArchUnitNET rule in `VsaRules` (`framework/Testing/Architecture/`). Use the next free `VSA2xxx` ID, and add a `[Test]` to `VsaArchitectureTests`. For a layer rule, change the layer map in `framework/Testing/Core/Layers.cs` and in `VSA.Layers.targets`.
4. Write a message that gives what is wrong, the type names, and the fix.
5. Add tests in `tests/Rules.Tests/`. For an analyzer, use the Roslyn analyzer test library (`AnalyzerTests.cs`). For an architecture rule, add a good and a bad fixture module in `tests/Fixtures/Rules/Modules/`.
6. Add the rule to the table in `docs/architecture-rules.md`.

## Working agreements

- Follow the conventions of the surrounding code (naming, structure, comment density).
- Add or update tests with every behavior change.
- Document notable architecture decisions in `docs/`.
- Keep this file current when conventions change.

<!-- TODO: stack, build/test commands, slice anatomy, naming rules — fill in as specs land. -->
