# Architecture rules

VSA has three enforcement levels. Each rule is in one level only.

| Level | When | Catches | IDs |
|---|---|---|---|
| `VSA.Sdk` | Before compile | Wrong references between layers and modules | `VSA0xxx` |
| `VSA.Analyzers` | During compile, in the IDE | Wrong code that one file shows | `VSA1xxx` |
| `VSA.Testing` | In a test run, after build | Rules over the whole app | `VSA2xxx` |

## Rules

| ID | Level | Rule |
|---|---|---|
| `VSA0001` | Sdk | A module project gets its references only from VSA.Sdk. |
| `VSA1001` | Analyzers | Requests and responses are sealed records. |
| `VSA1002` | Analyzers | Event handlers do not send requests: no request type and no `IBus` in a class that inherits `IntegrationEventHandler<T>`. |
| `VSA2001` | Testing | Each request has exactly one handler. |
| `VSA2002` | Testing | Two requests do not use the same method and route. The compare ignores case, a trailing `/` and the names of route parameters. |
| `VSA2003` | Testing | Each module root has exactly one `Module` class, and it is public and sealed. |

## Decisions

1. **Analyzers or Testing.** If one file shows the problem, the rule is an analyzer, because the IDE shows it at once. If the rule needs more than one assembly or more than one module, the rule is in `VSA.Testing`. VSA2003 is in Testing: a compilation-end analyzer shows only on a full build.
2. **Plain reflection, not ArchUnitNET.** Consumers get no new dependency. The rules read generic bases (`RoutedHandler<TRequest, TResponse>`), static abstract members (`TRequest.Method`, `TRequest.Route`) and assembly metadata. We control the message text.
3. **How the test finds the app.** A consumer writes `VsaArchitectureTests<Program>`. VSA.Testing reads all project libraries in the `DependencyContext` of the test run. A module root is an assembly with `AssemblyMetadata("VsaModule", "<name>")`. Its layers come from `AssemblyMetadata("VsaLayer", "<assembly>")`. The host uses the same records at start-up.

## Rule code

A test rule is a class that implements `IVsaRule`. `Check(VsaApp app)` returns one `Violation` for each problem. A rule has no test framework type, so a fixture test can give it any `VsaApp`.

Each message gives three things:

- what is wrong,
- the full names of the types,
- the fix.
