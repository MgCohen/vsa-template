# AGENTS.md

Instructions for AI coding agents (and humans) working in this repository. This file is model-agnostic: do not add tool-specific files such as `CLAUDE.md`; put shared guidance here.

## Project

A Vertical Slice Architecture (VSA) template: base infrastructure, scaffolding, a sample feature, and architecture rules, meant to be reused as the starting point for new projects.

## Layout

| Path     | Purpose                                              |
|----------|------------------------------------------------------|
| `src/`   | Application code, organized by feature (vertical slice) |
| `tests/` | Unit, integration, and architecture tests            |
| `docs/`  | Architecture decisions, conventions, and guides      |

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
