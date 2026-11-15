# VSA checks

The VSA checks ship with the framework and run on the modules of the consumer. Libraries do the work: the .NET analyzers, Roslyn, ArchUnitNET, NUnit and NSubstitute. VSA adds base classes and one architecture definition (`packs/Vsa/Architecture/VsaArchitecture.cs`, see `architecture-engine.md`).

## Levels

| Group | Level | Package | IDs |
|---|---|---|---|
| Editor and build | Analyzers | Built-in .NET analyzers + `Umpire.Vsa.Analyzers` (`packs/Vsa/Tests/Analyzers`, with `Umpire.Kernel.Analyzers`) | `CAxxxx`, `IDExxxx`, `UMP1xxx` |
| Build of the host | Architecture | `Umpire.Engine.Build` (rule engine, ArchUnitNET) | `UMP2xxx` |
| Tests | Unit | `Umpire.Vsa.Testing.Unit` (NUnit, NSubstitute) | None |
| Tests | End-to-end | `Umpire.Vsa.Testing.EndToEnd` | Not started |

The editor and the build run the first group in each consumer project. The build of the host project runs the architecture checks. The last group runs in a consumer test project.

## Rules

| ID | Level | Rule |
|---|---|---|
| `UMP1001` | Analyzers | Requests and responses are sealed records. Option: `check_responses`. |
| `UMP1002` | Analyzers | Event handlers do not use a request type or `IBus`. |
| `UMP1003` | Analyzers | A contracts project holds only requests, responses, commands, events and the types that they use. |
| `UMP1004` | Analyzers | Presenters do not use `IIntegrationEventPublisher`. A presenter shows events (`IPresenterEvent`); it does not publish them. |
| `UMP1005` | Analyzers | A `Domain.Shared` type is immutable: no `set` accessor (`init` is permitted), and each field is `readonly` or `const`. Trial (D-37). |
| `UMP1006` | Analyzers | A `Domain.Shared` type holds only data: no method, operator, conversion or constructor body, and no property body that calls a method. Trial (D-37). |
| `UMP1007` | Analyzers | Each base of a `Domain.Shared` type is in a `.Domain.Shared` assembly or in the option `allowed_bases` (empty in `Umpire.Rules.globalconfig`: an interface such as `IComparable<T>` needs a method that `UMP1006` does not permit). Trial (D-37). |
| `UMP1000` | Analyzers | Each option of each running VSA rule is in the config, with a valid value. |
| `UMP1008` | Analyzers | A line has at most `max` characters. Formatting, warning. |
| `UMP1009` | Analyzers | A method, constructor, accessor or local function has at most `max` lines. Formatting, warning. |
| `UMP1010` | Analyzers | A file has at most `max` lines. Formatting, warning. |
| `UMP1011` | Analyzers | A file has one top-level type. Option `allow_same_name` permits `Module` and `Module<T>`. Formatting, warning. |
| `UMP1012` | Analyzers | Members are ordered by kind, then by access, then static before instance. Options: `kind_order`, `access_order`, `static_first`. Formatting, warning. |
| `UMP1013` | Analyzers | The code has no comments: no `//`, no `/* */`, no `///` and no `/** */`, also after a directive such as `#pragma`. A directive, for example `#region`, is not a comment. Code in an inactive `#if` is not checked. Formatting, warning. |
| `UMP1014` | Analyzers | The root class of a plugin module inherits `Plugin` or `Plugin<TSettings>`, and the root class of a module inherits `Module` or `Module<TSettings>`. The host also checks it at startup. |
| `UMP1015` | Analyzers | The name of a class that inherits a hook base (`BeforeCommandHook<>`, `AfterCommandHook<,>`, `BeforeEventHook<>`, `AfterEventHook<>`) ends with `Hook`. |
| `UMP2001` | Architecture | Each type uses only the parts that the VSA definition permits. A plugin module is a base, and it uses no module. |
| `UMP2003` | Architecture | Each type exposes another part only within the access of its rule, for example a handler uses `Domain` but does not expose it outside its assembly. |
| `UMP2101` | Architecture | Each type is assignable to the `OfType` of its part, for example a handler in `Handlers.Requests` inherits `RequestHandler<,>`. |
| `UMP2102` | Architecture | Each type of the app is in a folder of a part. |
| `UMP2103` | Architecture | A warning: a Unity project has no `Library/ScriptAssemblies/` folder, so the checks do not read the code that only Unity compiles. |

## Plugin modules

A plugin module has the layers of a module and the folder layout of the framework: one folder for each layer, and no slices (see `slice-layout.md`). It is for the base types of the app, as the VSA pack (`packs/Vsa`) is the base of each layer. The VSA definition gives its rules:

| Rule | Check |
|---|---|
| A plugin module uses no feature module | `UMP2001` |
| The `Domain` of a plugin module uses no `Domain` of another plugin module | `UMP2001` |
| A module root uses the plugin modules | `UMP2001` |
| `Infrastructure` uses the `Handlers` and `Contracts` parts (the pipeline step bases), not their child parts | `UMP2001` |
| The root class of a plugin inherits `Plugin` | `UMP1014`, and the host at startup |

The project references do not enforce the layers. Umpire.Sdk gives each project the base of each layer, and the rule engine checks each use after the build (see `architecture-engine.md`).

The host finds the module roots from the types, with no build records:

| Fact | Source |
|---|---|
| Module root | A public, top-level class that inherits `Module` or `Plugin` |
| Module name | The last part of the namespace of the root class |
| Plugin module | The root class inherits `Plugin` |

One assembly can have many modules. Two root classes in one module namespace stop startup. A nested or internal class that inherits `Module`, for example in a test, does not make a module root.

The host configures the plugin modules first, then the other modules. In each group the order is by name. The host gets the plugin roots through the module roots, so an app with no module does not load its plugin modules.

## Analyzers: `VsaAnalyzer` and `UmpireAnalyzerRule`

`VsaAnalyzer` is the one analyzer. It finds each `UmpireAnalyzerRule` of the assembly, ignores generated code, and does not run a rule with severity `none`. A rule gives one descriptor and registers its actions. A rule option wins over a global option.

```csharp
public sealed class SealedRecordRule() : UmpireAnalyzerRule(Descriptor) {
    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSymbolAction(symbol => {
            if (Options(symbol.Options, symbol.Symbol.Locations[0].SourceTree!).Get("check_responses", fallback: true)) { ... }
        }, SymbolKind.NamedType);
}
```

```ini
dotnet_diagnostic.UMP1001.severity = warning   # standard severity key
umpire.check_responses = false                    # global option
umpire.UMP1001.check_responses = true             # rule option
```

## Rule files

| File | Holds |
|---|---|
| `Umpire.globalconfig` | The built-in .NET rules: editor and code style options, severities |
| `Umpire.Tests.globalconfig` | The built-in rules that are off in a project whose name ends with `.Tests` (CA1707, underscores in test names) |
| `Umpire.Rules.globalconfig` | The values of the Umpire rules. No rule has a default in code |
| `Umpire.Rules.props` | The MSBuild setup: both files, `EnforceCodeStyleInBuild`, the formatting rules as warnings |

`Sdk.props` imports `Umpire.Rules.props` for each Umpire.Sdk project. The root `Directory.Build.props` imports it for the framework and its tests, so this repository follows the same rules. `Directory.Build.targets` gives the framework projects the VSA analyzer with `UmpireFrameworkCode = true`: then only the rules about any code run (`IsModuleRule` is false), not the module rules. `Umpire.Vsa.Analyzers` cannot check itself.

A rule lists its options in `RequiredOptions`. At the start of a compilation, `VsaAnalyzer` checks each one in the global config. If an option is missing or not valid, it reports `UMP1000` (error), and that rule does not run. A consumer changes a value in its own `.editorconfig`.

## Formatting rules

The formatting rules are warnings, so a build does not fail on them (`WarningsNotAsErrors` in `Umpire.Rules.props`).

| Rule | Checks | Option |
|---|---|---|
| `IDE0055` | Each brace on its own line (Allman) | `csharp_new_line_before_open_brace` |
| `IDE0011` | Braces on each `if`, `for`, `while` | `csharp_prefer_braces` |
| `IDE2000` | No two blank lines in a row | `dotnet_style_allow_multiple_blank_lines_experimental` |
| `IDE0008` | Explicit type, except where the line shows the type | `csharp_style_var_*` |
| `IDE0040` | Access modifier on each member, except in an interface | `dotnet_style_require_accessibility_modifiers` |
| `IDE0055` | One statement on each line | `csharp_preserve_single_line_statements` |
| `IDE2002` | No blank line between two closing braces | `csharp_style_allow_blank_lines_between_consecutive_braces_experimental` |
| `IDE0065` | `using` inside the namespace | `csharp_using_directive_placement` |
| `IDE0036` | Modifiers in the default order | `csharp_preferred_modifier_order` |
| `IDE0049` | `int`, not `Int32` (editor only) | `dotnet_style_predefined_type_*` |
| `IDE0054` | `x += 1`, not `x = x + 1` | `dotnet_style_prefer_compound_assignment` |
| `IDE1005` | `Changed?.Invoke(...)` | `csharp_style_conditional_delegate_call` |
| `IDE0083` | `is not`, not `!(x is T)` | `csharp_style_prefer_not_pattern` |
| `IDE0021`, `IDE0022`, `IDE0061` | Block body for constructors, methods and local functions | `csharp_style_expression_bodied_*` |
| `IDE0023`–`IDE0027`, `IDE0053` | `=>` for operators, properties, indexers, accessors and lambdas when it fits on one line (`IDE0053` editor only) | `csharp_style_expression_bodied_*` |
| `IDE1006` | Names: `I` + PascalCase interfaces, `T` + PascalCase type parameters, PascalCase types, members and constants, camelCase private fields, parameters and locals | `dotnet_naming_*` |
| `IDE0051`, `IDE0052` | No private member that no code uses, or that code writes and never reads | |
| `IDE0059` | No value that no code reads. Fix: `_ = Save(order);` | `csharp_style_unused_value_assignment_preference` |
| `IDE0060` | No parameter that no code uses | `dotnet_code_quality_unused_parameters` |
| `IDE0005` | No `using` that the file does not need | `GenerateDocumentationFile` in `Umpire.Rules.props` |
| `IDE0004` | No cast to the type that the value already has | |
| `IDE0079` | No suppression that hides no warning (editor only) | |
| `CA1052`, `CA1012` | A class with only static members is `static`. An abstract class has no public constructor | `dotnet_code_quality.<ID>.api_surface` |
| `CA2000` | Dispose each `IDisposable` object | |
| `CA1849` | No blocking call, such as `.Result`, in an `async` method | |
| `CA1812` | An internal or private class that no code creates | A type with a reflection base is not reported. Handlers and the types that dependency injection creates give false warnings until issue #41 is closed |
| `UMP1008` | Characters in a line | `umpire.UMP1008.max` |
| `UMP1009` | Lines in a method | `umpire.UMP1009.max` |
| `UMP1010` | Lines in a file | `umpire.UMP1010.max` |
| `UMP1011` | One top-level type in a file | `umpire.UMP1011.allow_same_name` |
| `UMP1012` | Member order | `umpire.UMP1012.kind_order`, `access_order`, `static_first` |
| `UMP1013` | No comments | |

A rule with a maximum inherits `LimitRule`, which reads the option `max`. In `kind_order` and `access_order`, a name that the list does not have comes after the names of the list.

| Test | Checks |
|---|---|
| `FormattingRuleTests` | Each VSA rule and its options, on small sources |
| `FormattingBuildTests` | The Umpire.Sdk setup, on the built fixture `tests/Fixtures/Built/Formatting`: each rule gives a warning, the build passes, and the migrations give no warning |
| `RuleOptionTests` | `UMP1000`, and each option of each rule is in `Umpire.Rules.globalconfig` |

The analyzer tests read the real `Umpire.Rules.globalconfig`. A test config overrides a value, because the rules file has `global_level = -1`.

EF Core writes the files in `Infrastructure/Migrations/`. Umpire.Sdk marks them as generated code (`Umpire.Migrations.globalconfig` in `obj/`), so no rule checks them.

## Reflection bases: `ReflectionBaseSuppressor`

Reflection can call a private member that no code calls, for example a method that a base class finds by its name. It can also set a field that no code writes. The rules for unused code then give false warnings.

Put `[ReflectionBase]` (package `Umpire.Markers`, namespace `Umpire.Markers`) on the base class or the interface. `ReflectionBaseSuppressor`, a Roslyn `DiagnosticSuppressor` in Umpire.Vsa.Analyzers, then hides the rules in the table below on each type that derives from it or implements it. The base can be indirect or generic, and an interface can inherit the marked interface. A nested type that derives from the base is also covered.

The suppression applies to all private members of the type. A private member that is really not used also gets no warning.

The suppressor finds the attribute by its name, `ReflectionBaseAttribute`. A project that cannot reference the package, or a third-party base, can declare its own attribute with that name.

```csharp
[ReflectionBase]
public abstract class Screen { }

public sealed class MainMenu : Screen
{
    private void OnShow() { }   // no IDE0051
}
```

| Suppression | Hides |
|---|---|
| `UMP9001` | `IDE0051`: a private member that no code uses |
| `UMP9002` | `IDE0052`: a private member that no code reads |
| `UMP9003` | `CS0649`: a field that no code writes |
| `UMP9004` | `CS0169`: a field that no code uses |
| `UMP9005` | `IDE0060`: a parameter that no code uses |
| `UMP9006` | `CA1822`: a member that can be static. Reflection that uses `BindingFlags.Instance` cannot find a static member |
| `UMP9007` | `CA1812`: an internal class that no code creates. `Activator` creates it |

A suppressor can hide a warning that `TreatWarningsAsErrors` makes an error, for example CS0649.

Each layer references `Umpire.Markers`. It is a separate package because Umpire.Vsa.Analyzers is private (`PrivateAssets="all"`): a DLL in it does not reach the host at runtime.

## Architecture: the rule engine

The rule engine binds each type of the app to a part of the VSA definition. Its checks give `UMP2001`, `UMP2003`, `UMP2101`, `UMP2102` and the warning `UMP2103`. The build hook package `Umpire.Engine.Build` on the host project runs it after each build. A test project does not check the architecture. See `architecture-engine.md` for the definition API, the binding and app rules.

## Unit: `VsaUnitTest`

A unit test sends a request to its handler with `Handle`, or a command to its handler with `Send`, without HTTP and without a database. Before each test, the base makes new services with the default fakes and calls `Configure`. A test class makes its mocks in `Configure`, and its tests use them through fields.

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
| `ICommandSender` | None | `Mock<ICommandSender>()` |
| Other services | None | `Mock<T>()` |

`Send` finds the one handler in the `Handlers.Commands` project of the module of the command. If the handler sends other commands, add `Mock<ICommandSender>()` in `Configure`.

Add or replace services before the first `Handle` or `Send` call. After it, the services are read-only, and a change fails.

## Add a rule

1. If a built-in .NET rule does the check, set its severity in `tools/sdk/Sdk/Umpire.globalconfig`.
2. If one file shows the problem, write a rule class that inherits `UmpireAnalyzerRule`. Use the next free `UMP1xxx` ID, and add it to `AnalyzerReleases.Unshipped.md`.
3. Else change the rules of the VSA definition (`packs/Vsa/Architecture/VsaArchitecture.cs`).
4. Add tests. For an analyzer, use `tests/Rules.Tests/AnalyzerTests.cs`. For the engine, add a case to `tests/Architecture.Tests` with the fixture apps in `tests/Fixtures/Architecture/`.
5. Add the rule to the table in this file.
