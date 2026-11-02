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
   ├─ Architecture/         VSA.Testing.Architecture: VsaArchitectureTests<TEntry>, helpers VsaRules, LayerRules
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

Each rule is one `[Test]` in `VsaArchitectureTests`, with an ArchUnitNET rule in the test. A consumer test project needs one class, and it gets each rule as a test. A rule that comes from data, such as the layer map, gets the data from a helper and writes the ArchUnitNET rule in the test:

```csharp
// VSA2001: one ArchUnitNET rule for each module project. The scope reports all failures.
using var scope = Assert.EnterMultipleScope();
foreach (var (project, forbidden) in LayerRules.Forbidden(App, LayerMap)) {
    Types().That().ResideInAssembly(project.Assembly)
        .Should().NotDependOnAny(Types().That().ResideInAssembly(forbidden[0], [.. forbidden.Skip(1)]))
        .Check(Architecture);
}
```

```csharp
// framework/Testing/Architecture/VsaArchitectureTests.cs: a new VSA rule
[Test]
public void VSA2003_handlers_are_internal() =>
    Classes().That().HaveNameEndingWith("Handler").Should().NotBePublic().Check(Architecture);

// The consumer: all VSA rules, plus its own app rules
public sealed class ArchitectureTests : VsaArchitectureTests<Program> {
    [Test]
    public void Presenters_are_sealed() =>
        Classes().That().HaveNameEndingWith("Presenter").Should().BeSealed().Check(Architecture);
}
```

| Member | Use |
|---|---|
| `App`, `Architecture`, `LayerMap` | Virtual: a fixture test gives other modules or another map |
| `VsaRules.Load` | Optional helper: load an app |
| `LayerRules.Forbidden` | Data: the forbidden assemblies of each module project |

A fixture test calls the base test on fixture modules, so the rule is written in one place only:

```csharp
Fails(FixtureApp.Of("OpenModuleClass").VSA2002_Module_classes_are_public_and_sealed);
```

- The test project references the host. The host references each module, so the test run gets every module project.
- `The_app_has_modules` fails if no module root is found. Without it, each rule passes with nothing to check.

## Unit

A unit test sends a request to its handler. `HandlerHost` finds the handler in the `Handlers.Requests` project of the same module, and creates it with the services of the test.

Before each test, the base creates a new `HandlerHost` and calls `Configure`. A test class makes its fakes and mocks in `Configure`, and its tests use them through fields.

```csharp
public sealed class CreateNotePublisherTests : VsaUnitTest {
    IIntegrationEventPublisher publisher = null!;

    protected override void Configure(IServiceCollection services) {
        publisher = Mock<IIntegrationEventPublisher>();
        publisher.PublishAsync(Arg.Any<NoteCreated>(), Arg.Any<CancellationToken>()).ThrowsAsync(new TimeoutException());
    }

    [Test]
    public void Failure_of_the_publisher_stops_the_request() =>
        Assert.ThrowsAsync<TimeoutException>(() => Handle(new CreateNoteRequest(author, "Title")));
}
```

| Member | Use |
|---|---|
| `Configure(services)` | Virtual: add or replace services for each test |
| `Mock<T>()` | An NSubstitute mock of `T` in the services |
| `Handle(request)` | Runs the handler of the request |
| `Repository<T, TId>()`, `Events` | The default fakes |

| Service | Default | Replace |
|---|---|---|
| `IRepository<T, TId>` | `InMemoryRepository<T, TId>` | `Mock<IRepository<T, TId>>()` |
| `IIntegrationEventPublisher` | `RecordingEventPublisher` | `Mock<IIntegrationEventPublisher>()` |
| `ILogger<T>` | Logging with no output | `services.AddLogging(...)` |
| Other services | None | `Mock<T>()` or `services.Add...` |

- Add or replace services before the first `Handle` call. After it, the services are read-only, and a change fails.
- The mock library is NSubstitute. It has a short syntax, it has no license problem, and it runs in .NET and in the Unity editor.
- `HandlerHost` has no NUnit type, so another test framework can use it.

## Tests of the checks

| Check | Test | Good case | Bad cases |
|---|---|---|---|
| `VSA1001`, `VSA1002` | `tests/Rules.Tests/AnalyzerTests.cs` | Code in the test | Code in the test, options |
| `VSA2001` | `tests/Rules.Tests/RuleTests.cs` | `Good` | `Good` with a smaller layer map |
| `VSA2002` | `tests/Rules.Tests/RuleTests.cs` | `Good` | `OpenModuleClass` |
| Unit base | `tests/Rules.Tests/UnitBaseTests.cs` | `Good`, `Configure`, `Mock<T>()` | A service added after `Handle` |

The fixture modules are in `tests/Fixtures/Rules/Modules/`. The Notes sample uses the checks as a consumer does: `samples/Notes/Tests`.
