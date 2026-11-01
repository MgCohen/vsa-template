# Architecture rules

VSA has three enforcement levels. Each rule is in one level only.

| Level | When | Catches | IDs |
|---|---|---|---|
| `VSA.Sdk` | Before compile | Wrong references between layers and modules | `VSA0xxx` |
| `VSA.Analyzers` | During compile, in the IDE | Wrong code that one file shows | `VSA1xxx` |
| `VSA.Testing.Architecture` | In a test run, after build | Rules over the whole app | `VSA2xxx` |

## Rules

| ID | Level | Rule |
|---|---|---|
| `VSA0001` | Sdk | A module project gets its references only from VSA.Sdk. |
| `VSA1001` | Analyzers | Requests and responses are sealed records. |
| `VSA1002` | Analyzers | Event handlers do not send requests: no request type and no `IBus` in a class that inherits `IntegrationEventHandler<T>`. |
| `VSA2001` | Architecture | Each request has exactly one handler. |
| `VSA2002` | Architecture | Two requests do not use the same method and route. The compare ignores case, a trailing `/` and the names of route parameters. |
| `VSA2003` | Architecture | Each module root has exactly one `Module` class, and it is public and sealed. |
| `VSA2004` | Architecture | Each module project uses only the VSA bases and module projects that the layer map permits. |

## Decisions

1. **Analyzers or Architecture.** If one file shows the problem, the rule is an analyzer, because the IDE shows it at once. If the rule needs more than one assembly or more than one module, the rule is in `VSA.Testing.Architecture`. VSA2003 is in Architecture: a compilation-end analyzer shows only on a full build.
2. **Plain reflection, not ArchUnitNET.** Consumers get no new dependency. The rules read generic bases (`RoutedHandler<TRequest, TResponse>`), static abstract members (`TRequest.Method`, `TRequest.Route`) and assembly metadata. We control the message text.
3. **How the test finds the app.** A consumer writes `VsaArchitectureTests<Program>`. VSA.Testing.Architecture reads all project libraries in the `DependencyContext` of the test run. A module root is an assembly with `AssemblyMetadata("VsaModule", "<name>")`. Its layers come from `AssemblyMetadata("VsaLayer", "<assembly>")`. The host uses the same records at start-up.

4. **One layer map.** The layer map is C# code in `VSA.Testing.Core` (`Layers.cs`). It copies the map in the VSA Layer Map artifact. The architecture rules read it. VSA.Sdk cannot run C# before compile, so `VSA.Layers.targets` has the same map in MSBuild. If you change one, change the other.

## Consumer use

```csharp
public sealed class ArchitectureTests : VsaArchitectureTests<Program>;
```

- The test project uses xUnit v3. VSA.Testing.Architecture references only `xunit.v3.extensibility.core`.
- The test project references the host and each module project. The host references the modules with `PrivateAssets="all"`, so the modules do not come from the host reference.
- `The_app_has_modules` fails if no module root is found. Without it, each rule passes with nothing to check.

## Rule code

A test rule is a class that implements `IVsaRule`. `Check(VsaApp app)` returns one `Violation` for each problem. A rule has no test framework type, so a fixture test can give it any `VsaApp`.

Each message gives three things:

- what is wrong,
- the full names of the types,
- the fix.

## Tests

| Rule | Test | Good case | Bad cases |
|---|---|---|---|
| `VSA1001`, `VSA1002` | `tests/Rules.Tests/AnalyzerTests.cs` | Code in the test | Code in the test |
| `VSA2001` | `tests/Rules.Tests/RuleTests.cs` | `Good` | `NoHandler`, `TwoHandlers` |
| `VSA2002` | `tests/Rules.Tests/RuleTests.cs` | `Good` | `SameRoute` |
| `VSA2003` | `tests/Rules.Tests/RuleTests.cs` | `Good` | `NoModuleClass`, `TwoModuleClasses`, `OpenModuleClass` |
| `VSA2004` | `tests/Rules.Tests/RuleTests.cs` | `Good` | `Good` with a smaller layer map |

The fixture modules are in `tests/Fixtures/Rules/Modules/`. Each test gives the rule only the modules that it names.
