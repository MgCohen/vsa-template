# VSA checks

The VSA checks ship with the framework and run on the modules of the consumer. Libraries do the work: the .NET analyzers, Roslyn, ArchUnitNET, NUnit and NSubstitute. VSA adds base classes and one layer map (`framework/Testing/Core/Layers.cs`).

## Levels

| Group | Level | Package | IDs |
|---|---|---|---|
| Editor and build | Build | `VSA.Sdk` (`framework/Sdk`) | `VSA0xxx` |
| Editor and build | Analyzers | Built-in .NET analyzers + `VSA.Analyzers` (`framework/Analyzers`) | `CAxxxx`, `IDExxxx`, `VSA1xxx` |
| Tests | Architecture | `VSA.Testing.Architecture` (ArchUnitNET) | `VSA2xxx` |
| Tests | Unit | `VSA.Testing.Unit` (NUnit, NSubstitute) | None |
| Tests | End-to-end | `VSA.Testing.EndToEnd` | Not started |

The editor and the build run the first group in each consumer project. The second group runs in a consumer test project.

## Rules

| ID | Level | Rule |
|---|---|---|
| `VSA0001` | Build | A module project gets its references only from VSA.Sdk. |
| `VSA1001` | Analyzers | Requests and responses are sealed records. Option: `check_responses`. |
| `VSA1002` | Analyzers | Event handlers do not use a request type or `IBus`. |
| `VSA2001` | Architecture | Module projects use only what the layer map permits. |
| `VSA2002` | Architecture | `Module` classes are public and sealed. |

The layer map is also in `framework/Sdk/Sdk/VSA.Layers.targets`, because VSA.Sdk cannot run C# before compile. `SdkLayerMapTests` fails if the two maps are different.

## Analyzers: `VsaAnalyzer` and `VsaAnalyzerRule`

`VsaAnalyzer` is the one analyzer. It finds each `VsaAnalyzerRule` of the assembly, ignores generated code, and does not run a rule with severity `none`. A rule gives one descriptor and registers its actions. A rule option wins over a global option.

```csharp
public sealed class SealedRecordRule() : VsaAnalyzerRule(Descriptor) {
    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSymbolAction(symbol => {
            if (Options(symbol.Options, symbol.Symbol.Locations[0].SourceTree!).Get("check_responses", fallback: true)) { ... }
        }, SymbolKind.NamedType);
}
```

```ini
dotnet_diagnostic.VSA1001.severity = warning   # standard severity key
vsa.check_responses = false                    # global option
vsa.VSA1001.check_responses = true             # rule option
```

## Architecture: `VsaArchitectureTests`

Each rule is one `[Test]` with an ArchUnitNET rule. A consumer test project references the host and has one class. It gets each rule as a test, and it can add its own rules.

```csharp
public sealed class ArchitectureTests : VsaArchitectureTests {
    [Test]
    public void Presenters_are_sealed() =>
        Classes().That().HaveNameEndingWith("Presenter").Should().BeSealed().Check(Architecture);
}
```

`App`, `LayerMap` and `Architecture` are virtual. A fixture test gives other modules or another layer map. `The_app_has_modules` fails if the test run has no module root, because then each rule passes with nothing to check.

## Unit: `VsaUnitTest`

A unit test sends a request to its handler, without HTTP and without a database. Before each test, the base makes new services with the default fakes and calls `Configure`. A test class makes its mocks in `Configure`, and its tests use them through fields.

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

| Service | Default | Seed or replace |
|---|---|---|
| `IRepository<T, TId>` | `InMemoryRepository<T, TId>` | `Repository<T, TId>().With(...)` |
| `IIntegrationEventPublisher` | `RecordingEventPublisher` (`Events`) | `Mock<IIntegrationEventPublisher>()` |
| `ILogger<T>` | Logging with no output | `services.AddLogging(...)` |
| Other services | None | `Mock<T>()` |

Add or replace services before the first `Handle` call. After it, the services are read-only, and a change fails.

## Add a rule

1. If a built-in .NET rule does the check, set its severity in `framework/Sdk/Sdk/VSA.globalconfig`.
2. If one file shows the problem, write a rule class that inherits `VsaAnalyzerRule`. Use the next free `VSA1xxx` ID, and add it to `AnalyzerReleases.Unshipped.md`.
3. Else add one `[Test]` with an ArchUnitNET rule to `VsaArchitectureTests`. Use the next free `VSA2xxx` ID in the test name.
4. Add tests in `tests/Rules.Tests/`. For an analyzer, use `AnalyzerTests.cs`. For an architecture rule, add a good and a bad fixture module in `tests/Fixtures/Rules/Modules/`, and call the base test:

   ```csharp
   Fails(FixtureApp.Of("OpenModuleClass").VSA2002_Module_classes_are_public_and_sealed);
   ```

5. Add the rule to the table in this file.
