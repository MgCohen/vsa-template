# VSA checks

The VSA checks ship with the framework and run on the consumer's modules. Libraries do the work: the .NET analyzers, Roslyn, ArchUnitNET and NUnit. VSA adds base classes and one layer map.

## Levels

| Level | Package | When | IDs |
|---|---|---|---|
| Build | `VSA.Sdk` | Before compile | `VSA0xxx` |
| Analyzers | Built-in .NET analyzers + `VSA.Analyzers` | During compile, in the IDE | `CAxxxx`, `IDExxxx`, `VSA1xxx` |
| Architecture | `VSA.Testing.Architecture` (ArchUnitNET) | In a test run | `VSA2xxx` |
| Unit | `VSA.Testing.Unit` (NUnit) | In a test run | None |
| End-to-end | `VSA.Testing.EndToEnd` | In a test run | Not started |

```
framework/
├─ Layers/                  the code
├─ Sdk/                     VSA.Sdk: build setup, layer references, analyzer settings (VSA.globalconfig)
└─ Testing/
   ├─ Core/                 VSA.Testing.Core: the layer map (Layer, Edge, Layers)
   ├─ Analyzers/            VSA.Analyzers: VsaAnalyzer base and the VSA1xxx rules
   ├─ Architecture/         VSA.Testing.Architecture: VsaArchitectureTests<TEntry>, VsaRules, LayerRules
   └─ Unit/                 VSA.Testing.Unit: VsaUnitTest, HandlerHost, default fakes
```

## Rules

| ID | Level | Rule |
|---|---|---|
| `VSA0001` | Build | A module project gets its references only from VSA.Sdk. |
| `VSA1001` | Analyzers | Requests and responses are sealed records. Option: `check_responses`. |
| `VSA1002` | Analyzers | Event handlers do not use a request type or `IBus`. |
| `VSA2001` | Architecture | Module projects use only what the layer map permits. |
| `VSA2002` | Architecture | `Module` classes are public and sealed. |

## Decisions

1. **Libraries, not an own rule system.** The built-in .NET analyzers do the basic rules (`AnalysisLevel = latest-recommended`). Roslyn does the custom rules. ArchUnitNET does the architecture rules. VSA code is only the base classes and the layer map.
2. **NUnit.** NUnit runs in .NET and in Unity. The Unity Test Framework uses NUnit, and xUnit does not run in Unity.
3. **One layer map.** `framework/Testing/Core/Layers.cs` copies the VSA Layer Map artifact. `LayerRules` makes one ArchUnitNET rule for each module project from it. VSA.Sdk cannot run C# before compile, so `VSA.Layers.targets` has the same map in MSBuild. If you change one, change the other.
4. **How the test finds the app.** VSA.Testing.Architecture uses `ModuleDiscovery.FindRoots()` of VSA.Module. The host uses the same API at start-up, so the tests and the host find the same modules.

## Analyzers

Each custom analyzer inherits `VsaAnalyzer`. The base registers the descriptor, ignores generated code and reads options.

```ini
# .editorconfig or .globalconfig of the consumer
dotnet_diagnostic.VSA1001.severity = warning   # standard severity key
vsa.check_responses = false                    # global option: all VSA rules that read the key
vsa.VSA1001.check_responses = true             # rule option: wins over the global option
```

```csharp
protected override void Register(AnalysisContext context) =>
    context.RegisterSymbolAction(symbol => {
        if (Options(symbol.Options, symbol.Symbol.Locations[0].SourceTree).Get("check_responses", fallback: true)) { ... }
    }, SymbolKind.NamedType);
```

## Architecture

A consumer test project needs one class. It gets each VSA2xxx rule as a test.

```csharp
public sealed class ArchitectureTests : VsaArchitectureTests<Program> {
    // An app rule: an ArchUnitNET rule, checked on Architecture.
    [Test]
    public void Handlers_are_internal() =>
        Classes().That().ResideInNamespace("Notes.Handlers.Requests").Should().NotBePublic().Check(Architecture);
}
```

- The test project references the host and each module project. The host references the modules with `PrivateAssets="all"`, so the modules do not come from the host reference.
- `The_app_has_modules` fails if no module root is found. Without it, each rule passes with nothing to check.

## Unit

A unit test sends a request to its handler. `HandlerHost` finds the handler in the `Handlers.Requests` project of the same module, and creates it with the services of the test.

```csharp
public sealed class CreateNoteTests : VsaUnitTest {
    [Test]
    public async Task Creates_the_note() {
        var result = await Handle(new CreateNoteRequest(author, "Title"));
        Assert.That(Repository<Note, NoteId>().Saved, Has.Count.EqualTo(1));
        Assert.That(Events.OfType<NoteCreated>(), Is.Not.Empty);
    }
}
```

| Service | Default | Replace |
|---|---|---|
| `IRepository<T, TId>` | `InMemoryRepository<T, TId>` | `Services.AddSingleton<IRepository<T, TId>>(...)` |
| `IIntegrationEventPublisher` | `RecordingEventPublisher` | `Services.AddSingleton<IIntegrationEventPublisher>(...)` |
| `ILogger<T>` | Logging with no output | `Services.AddLogging(...)` |

Add or replace services before the first `Handle` call. `HandlerHost` has no NUnit type, so another test framework can use it.

## Tests of the checks

| Check | Test | Good case | Bad cases |
|---|---|---|---|
| `VSA1001`, `VSA1002` | `tests/Rules.Tests/AnalyzerTests.cs` | Code in the test | Code in the test, options |
| `VSA2001` | `tests/Rules.Tests/RuleTests.cs` | `Good` | `Good` with a smaller layer map |
| `VSA2002` | `tests/Rules.Tests/RuleTests.cs` | `Good` | `OpenModuleClass` |
| Unit base | `tests/Rules.Tests/UnitBaseTests.cs` | `Good` | None |

The fixture modules are in `tests/Fixtures/Rules/Modules/`. The Notes sample uses the checks as a consumer does: `samples/Notes/Tests`.
