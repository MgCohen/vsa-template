# AGENTS.md

Instructions for AI coding agents (and humans) working in this repository. This file is model-agnostic: do not add tool-specific files such as `CLAUDE.md`; put shared guidance here.

## Project

A Vertical Slice Architecture (VSA) template: base infrastructure, scaffolding, a sample feature, and architecture rules, meant to be reused as the starting point for new projects.

## Layout

| Path                  | Purpose                                                                 |
|-----------------------|-------------------------------------------------------------------------|
| `framework/Layers/`   | The framework code: one project for each layer base. Ships as NuGet packages |
| `framework/Rules/`    | The enforcement: `Sdk/` (VSA.Sdk, the layer references), `Analyzers/` |
| `framework/Tooling/`  | The `vsa` tool (`Cli/`, package VSA.Cli) and its templates (`Cli/Templates/`) |
| `samples/`            | Sample apps that use the framework as a consumer does. Does not ship   |
| `tests/`              | Tests of the framework itself, with fixtures. Does not ship            |
| `docs/`               | Architecture decisions, conventions, and guides                         |

A module project is under a `Modules/` folder. Its references come from VSA.Sdk, from the folder of the project. Do not add references to a module project by hand: the build fails with `VSA0001`. Inside this repository, the sample projects import VSA.Sdk from source (`samples/VSA.Sdk.Dev.props`). A consumer project is only `<Project Sdk="VSA.Sdk" />`.

## Core principles

- **Organize by feature, not by layer.** Each slice owns its request, handler, validation, and endpoint in one place.
- **Slices are independent.** A slice does not reach into another slice's internals; share only through explicit, minimal shared code.
- **Keep shared code small.** Promote code to a shared location only when two or more slices genuinely need it.
- **Rules are enforced by tests.** Architecture rules live in `tests/` and must pass.

## Working agreements

- Follow the conventions of the surrounding code (naming, structure, comment density).
- Add or update tests with every behavior change.
- Document notable architecture decisions in `docs/`.
- Keep this file current when conventions change.

<!-- TODO: stack, build/test commands, slice anatomy, naming rules — fill in as specs land. -->

## Scaffolding: the vsa tool

The `vsa` tool (package `VSA.Cli`) writes new apps, modules and requests. It writes only new files: it does not write over a file, and it does not edit a file, except with `dotnet sln add`.

```bash
dotnet tool exec VSA.Cli new app <Name>          # global.json, nuget.config, dotnet-tools.json, <Name>.slnx, AGENTS.md, App/, Modules/
dotnet vsa add module <Name>                     # all layers, a status request, a presenter and a page
dotnet vsa add request <Name> -m <Module>        # the request, the response and the handler
dotnet vsa doctor                                # the tool, VSA.Sdk and the VSA.* packages have one version
```

The templates are in `framework/Tooling/Cli/Templates/`. They are one app that builds in `VSA.slnx`, with the sample names `SampleApp`, `SampleModule` and `SampleAction`. The tool replaces these names. A template file with `SampleAction` in its path belongs to `add request`. A file under `Modules/SampleModule/`, or with `SampleModule` in its file name, belongs to `add module`. All other files belong to `new app`. `tests/Cli` lists each template file with its command. If you change a template, build `VSA.slnx` and run the tests in `tests/Cli`. See `docs/scaffolding.md`.
