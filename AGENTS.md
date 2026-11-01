# AGENTS.md

Instructions for AI coding agents (and humans) working in this repository. This file is model-agnostic: do not add tool-specific files such as `CLAUDE.md`; put shared guidance here.

## Project

A Vertical Slice Architecture (VSA) template: base infrastructure, scaffolding, a sample feature, and architecture rules, meant to be reused as the starting point for new projects.

## Layout

| Path                  | Purpose                                                                 |
|-----------------------|-------------------------------------------------------------------------|
| `framework/Layers/`   | The framework code: one project for each layer base. Ships as NuGet packages |
| `framework/Sdk/`      | VSA.Sdk: the build setup of each module project and the layer references |
| `framework/Testing/`  | The checks that ship: `Core/` (VSA.Testing.Core, the layer map), `Analyzers/` (VSA.Analyzers), `Architecture/` (VSA.Testing.Architecture) |
| `samples/`            | Sample apps that use the framework as a consumer does. Does not ship   |
| `tests/`              | Tests of the framework itself, with fixtures. Does not ship            |
| `docs/`               | Architecture decisions, conventions, and guides                         |

A module project is under a `Modules/` folder. Its references come from VSA.Sdk, from the folder of the project. Do not add references to a module project by hand: the build fails with `VSA0001`. Inside this repository, the sample projects import VSA.Sdk from source (`samples/VSA.Sdk.Dev.props`). A consumer project is only `<Project Sdk="VSA.Sdk" />`.

## Core principles

- **Organize by feature, not by layer.** Each slice owns its request, handler, validation, and endpoint in one place.
- **Slices are independent.** A slice does not reach into another slice's internals; share only through explicit, minimal shared code.
- **Keep shared code small.** Promote code to a shared location only when two or more slices genuinely need it.
- **Rules are enforced by tests.** Architecture rules live in `tests/` and must pass.

## Add an architecture rule

The levels, IDs and rules are in `docs/architecture-rules.md`. A consumer gets all VSA.Testing.Architecture rules with one class in an xUnit v3 test project: `public sealed class ArchitectureTests : VsaArchitectureTests<Program>;`. VSA.Testing.Architecture needs xUnit v3.

1. If one file shows the problem, write an analyzer in `framework/Testing/Analyzers/`. Use the next free `VSA1xxx` ID and add it to `AnalyzerReleases.Unshipped.md`.
2. Else write a class that implements `IVsaRule` in `framework/Testing/Architecture/Rules/`. Use the next free `VSA2xxx` ID, add it to `VsaRules.All`, and add a `[Fact]` to `VsaArchitectureTests`. Do not use test framework types in the rule. If the rule is about layers, read the layer map in `framework/Testing/Core/Layers.cs`.
3. Write a message that gives what is wrong, the type names, and the fix.
4. Add tests in `tests/Rules.Tests/`. For an analyzer, use the Roslyn analyzer test library (`AnalyzerTests.cs`). For an architecture rule, add a good and a bad fixture module in `tests/Fixtures/Rules/Modules/`.
5. Add the rule to the table in `docs/architecture-rules.md`.

## Working agreements

- Follow the conventions of the surrounding code (naming, structure, comment density).
- Add or update tests with every behavior change.
- Document notable architecture decisions in `docs/`.
- Keep this file current when conventions change.

<!-- TODO: stack, build/test commands, slice anatomy, naming rules — fill in as specs land. -->
