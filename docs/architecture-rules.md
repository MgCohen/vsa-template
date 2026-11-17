# VSA checks

The VSA checks ship with the framework and run on the modules of the consumer. Libraries do the work: the .NET analyzers, Roslyn, ArchUnitNET, NUnit and NSubstitute. VSA adds base classes and one architecture definition (`packs/Vsa/Architecture/VsaArchitecture.cs`, see `architecture-engine.md`).

## Levels

| Group | Level | Package | IDs |
|---|---|---|---|
| Editor and build | Analyzers | Built-in .NET analyzers + `Umpire.Vsa.Analyzers` (`packs/Vsa/Tests/Analyzers`, with `Umpire.Kernel.Analyzers`) | `CAxxxx`, `IDExxxx`, `UMP01xxxx` |
| Build of the host | Architecture | `Umpire.Engine.Build` (rule engine, ArchUnitNET) | `UMP02xxxx` |
| Tests | Unit | `Umpire.Vsa.Testing.Unit` (NUnit, NSubstitute) | None |
| Tests | End-to-end | `Umpire.Vsa.Testing.EndToEnd` | Not started |

The editor and the build run the first group in each consumer project. The build of the host project runs the architecture checks. The last group runs in a consumer test project.

## Rules

| ID | Level | Rule |
|---|---|---|
| `UMP010201` | Analyzers | Requests and responses are sealed records. Option: `check_responses`. |
| `UMP010202` | Analyzers | Event handlers do not use a request type or `IBus`. |
| `UMP010203` | Analyzers | A contracts project holds only requests, responses, commands, events and the types that they use. Disabled (#143). |
| `UMP010204` | Analyzers | Presenters do not use `IIntegrationEventPublisher`. A presenter shows events (`IPresenterEvent`); it does not publish them. |
| `UMP010205` | Analyzers | A `Domain.Shared` type is immutable: no `set` accessor (`init` is permitted), and each field is `readonly` or `const`. Disabled (#143). |
| `UMP010206` | Analyzers | A `Domain.Shared` type holds only data: no method, operator, conversion or constructor body, and no property body that calls a method. Disabled (#143). |
| `UMP010207` | Analyzers | Each base of a `Domain.Shared` type is in a `.Domain.Shared` assembly or in the option `allowed_bases` (empty in `Umpire.Vsa.Analyzers.globalconfig`: an interface such as `IComparable<T>` needs a method that `UMP010206` does not permit). Disabled (#143). |
| `UMP010001` | Analyzers | Each option of each running rule has a valid value: from the config, or the default of its rule. |
| `UMP010002` | Build (warning) | The `.umpireconfig` file has no section. The analyzers read only the keys above the first section. |
| `UMP010101` | Analyzers | A line has at most `max` characters. Formatting, warning. |
| `UMP010102` | Analyzers | A method, constructor, accessor or local function has at most `max` lines. Formatting, warning. |
| `UMP010103` | Analyzers | A file has at most `max` lines. Formatting, warning. |
| `UMP010104` | Analyzers | A file has one top-level type. Option `allow_same_name` permits `Module` and `Module<T>`. Formatting, warning. |
| `UMP010105` | Analyzers | Members are ordered by kind, then by access, then static before instance. Options: `kind_order`, `access_order`, `static_first`. Formatting, warning. |
| `UMP010106` | Analyzers | The code has no comments: no `//`, no `/* */`, no `///` and no `/** */`, also after a directive such as `#pragma`. A directive, for example `#region`, is not a comment. Code in an inactive `#if` is not checked. Formatting, warning. |
| `UMP010209` | Analyzers | The name of a class that inherits a hook base (`BeforeCommandHook<>`, `AfterCommandHook<,>`, `BeforeEventHook<>`, `AfterEventHook<>`) ends with `Hook`. |
| `UMP020101` | Architecture | Each type uses only the parts that the VSA definition permits. A plugin module is a base, and it uses no module. |
| `UMP020102` | Architecture | Each type exposes another part only within the access of its rule, for example a handler uses `Domain` but does not expose it outside its assembly. |
| `UMP020201` | Architecture | Each type is assignable to the `OfType` of its part, for example a handler in `Handlers.Requests` inherits `RequestHandler<,>`. |
| `UMP020202` | Architecture | Each type of the app is in a folder of a part. |
| `UMP020203` | Architecture | A warning: a Unity project has no `Library/ScriptAssemblies/` folder, so the checks do not read the code that only Unity compiles. |
| `UMP020301` | Architecture | Each count of the definition is in its limit, for example each slice has at least one handler. See "Counts and matches". |
| `UMP020302` | Architecture | Each match of the definition holds, for example a request handler handles a request of its own slice. See "Counts and matches". |
| `UMP020401` | Architecture | No part of the definition covers the folder of another block. Mark the folder with `AddBlock<T>()` or `AddBlockGroup<T>()`. |
| `UMP020402` | Architecture | Each block place holds a check entry. |
| `UMP03xxxx` | Engine | The engine run: a step throws or is skipped, the check entries, a version, the load of the app, the baseline. One table: "Engine codes" in `architecture-engine.md`. |

Each ID is `UMP` and six digits: the area (2), the group (2) and the case (2). Area `01` is the analyzers: group `00` the base (`UMP010001`), `01` Umpire.Kernel.Analyzers, `02` Umpire.Vsa.Analyzers. Area `02` is the rule findings of the engine: `01` uses, `02` binding, `03` counts and matches, `04` block places. Area `03` is the engine run: group `00` the run, then one group for each step. `tests/Rules.Tests/CodeTests.cs` and `tests/Engine.Tests/CodeTests.cs` check the form.

## Disabled rules

If the design of an analyzer or a rule is not clear, it is disabled, not deleted. Each disabled rule has a row in issue #143, and the review of all rows comes before 0.1.0. The globalconfig file of its analyzer package sets the severity of each one to `none`, and `RuleAnalyzer` does not run a rule with severity `none`. The code and the tests of each rule stay. A consumer can turn a rule on in the `.umpireconfig` file of its block.

| Code | Class | Why disabled | Issue |
|---|---|---|---|
| `UMP010203` | `ContractTypeRule` | It reads `UmpireRootFolder` and an assembly suffix, so it never runs in a one-project app. The binding and use rules of the engine can cover it | #143 |
| `UMP010205` | `ImmutableSharedTypeRule` | The code shape (what a type can contain) is not decided | #142, #143 |
| `UMP010206` | `DataOnlySharedTypeRule` | The code shape is not decided | #142, #143 |
| `UMP010207` | `SharedTypeBaseRule` | The code shape is not decided | #142, #143 |

`DisabledRuleTests` makes sure that the codes with severity `none` in `Umpire.Vsa.Analyzers.globalconfig` are the codes of this table. To disable a rule, add its severity line and a row here and in #143.

## Plugin modules

A plugin module has the layers of a module and the folder layout of the framework: one folder for each layer, and no slices (see `slice-layout.md`). It is for the base types of the app, as the VSA pack (`packs/Vsa`) is the base of each layer. The VSA definition gives its rules:

| Rule | Check |
|---|---|
| A plugin module uses no feature module | `UMP020101` |
| The `Domain` of a plugin module uses no `Domain` of another plugin module | `UMP020101` |
| A module root uses the plugin modules | `UMP020101` |
| `Infrastructure` uses the `Handlers` and `Contracts` parts (the pipeline step bases), not their child parts | `UMP020101` |
| The root class of a plugin inherits `Plugin` | `UMP020101` and `UMP020301`, and the host at startup |
| The root class of a feature module does not inherit `Plugin` (`Module Under<Feature> denies Composition`) | `UMP020101`, and the host at startup |
| A plugin module has exactly one root class | `UMP020301`, and the host at startup |
| A plugin layer uses the same layer of another plugin module (the plugin group is open) | Permitted |
| A plugin module has no `Slices/` folder | `UMP020202` |

The project references do not enforce the layers. Umpire.Sdk gives each project the base of each layer, and the rule engine checks each use after the build (see `architecture-engine.md`).

The host finds the module roots from the types, with no build records:

| Fact | Source |
|---|---|
| Module root | A public, top-level class that inherits `Module` or `Plugin` |
| Module name | The last part of the namespace of the root class |
| Plugin module | The root class inherits `Plugin` |

One assembly can have many modules. Two root classes in one module namespace stop startup. A nested or internal class that inherits `Module`, for example in a test, does not make a module root.

The host configures the plugin modules first, then the other modules. In each group the order is by name. The host gets the plugin roots through the module roots, so an app with no module does not load its plugin modules.

## Analyzers: `RuleAnalyzer` and `UmpireAnalyzerRule`

`VsaAnalyzer` and `KernelAnalyzer` each find each `UmpireAnalyzerRule` of their assembly, ignore generated code, and do not run a rule with severity `none`. A rule sets its number, its text and the defaults of its options, and registers its actions. A rule option wins over a global option.

```csharp
public sealed class SealedRecordRule : VsaRule {
    public const string CheckResponsesKey = "check_responses";

    public override int Number => 1;

    public override IReadOnlyList<UmpireOption> Options { get; } = [UmpireOption.Bool(CheckResponsesKey, true)];

    protected override string Title => "Requests and responses are sealed records";

    protected override string Message => "{0} is {1}. Make it a sealed record.";

    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSymbolAction(symbol => {
            if (Values(symbol.Options, symbol.Symbol.Locations[0].SourceTree!).Bool(CheckResponsesKey) == true) { ... }
        }, SymbolKind.NamedType);
}
```

```ini
dotnet_diagnostic.UMP010201.severity = warning   # standard severity key
umpire.check_responses = false                    # global option
umpire.UMP010201.check_responses = true             # rule option
```

## Rule files

| File | Holds |
|---|---|
| `.editorconfig` (repository root) | The style of this repository: the built-in .NET rules, editor and code style options, severities. A test section turns off CA1707 (underscores in test names). No `UMP` key |
| `framework/Kernel/Analyzers/Umpire.Kernel.Analyzers.globalconfig` | The help text of the keys of the kernel rules (`UMP0101xx`). The defaults are on the rule classes |
| `packs/Vsa/Tests/Analyzers/Umpire.Vsa.Analyzers.globalconfig` | The severities of the disabled VSA rules (`UMP0102xx`) and the help text of their keys |
| `Directory.Build.props` (repository root) | The style switches of this repository: `EnforceCodeStyleInBuild`, `GenerateDocumentationFile`, the formatting rules as warnings |
| `framework/Kernel/Analyzers/Umpire.RuleValues.targets` | The one place that knows how the rule values reach a compile |

Each analyzer package holds `build/<PackageId>.globalconfig` and `build/<PackageId>.props`. NuGet imports the props file in each project that references the package, and it gives the values to the compiler. In source mode, `Sdk.targets` and `Directory.Build.targets` add the same files from the analyzer folders. The definition holds no analyzer value, and the analyzer files hold no definition option.

Umpire.Sdk ships no native style: no .NET rule severity, no `TreatWarningsAsErrors` and no `AnalysisLevel`. A consumer app sets its own style in its `.editorconfig`. The root `.editorconfig` and `Directory.Build.props` are the style of this repository only. `tools/sdk/Umpire.Sdk.Dev.props` also sets `AnalysisLevel = latest-recommended` for the apps of this repository (the engine, the fixtures and the templates). `Directory.Build.targets` gives the framework projects the kernel analyzer and its rule values, with `UmpireFrameworkCode = true`: then only the rules about any code run (`IsModuleRule` is false), not the module rules. `Umpire.Vsa.Analyzers` cannot check itself.

Each analyzer rule is one class on a group base: `KernelRule` (group `01`) or `VsaRule` (group `02`). The rule sets only its `Number`, its `Title`, its `Message` and the defaults of its options. The base makes the ID (`UMP01` + group + number) and the descriptor. Tests and other code find a rule by its type, for example `new LineLengthRule().Rule`.

A rule lists its options in `Options`, each with its default on the class. The lookup is: `umpire.<rule ID>.<key>`, then `umpire.<key>`, then the default. At the start of a compilation, `RuleAnalyzer` checks each option. If a value is not valid, or an option has no default and no value, it reports `UMP010001` (error), and that rule does not run. A consumer changes a value in the `.umpireconfig` file of its block.

## Formatting rules

In this repository, the formatting rules are warnings, so a build does not fail on them (`WarningsNotAsErrors` in the root `Directory.Build.props`).

| Rule | Checks | Option |
|---|---|---|
| `IDE0055` | Relaxed until 0.1.0: each brace on the line of its statement, and `else`, `catch` and `finally` on the line of the closing brace. Target value: each brace on its own line (Allman), and `else`, `catch` and `finally` on a new line | `csharp_new_line_before_open_brace`, `csharp_new_line_before_else`, `csharp_new_line_before_catch`, `csharp_new_line_before_finally` |
| `IDE0011` | Braces on each `if`, `for`, `while` | `csharp_prefer_braces` |
| `IDE2000` | No two blank lines in a row | `dotnet_style_allow_multiple_blank_lines_experimental` |
| `IDE0008` | Relaxed until 0.1.0: `var` is correct everywhere. Target value: explicit type, except where the line shows the type | `csharp_style_var_*` |
| `IDE0040` | Relaxed until 0.1.0: off. Target value: access modifier on each member, except in an interface | `dotnet_style_require_accessibility_modifiers` |
| `IDE0055` | One statement on each line | `csharp_preserve_single_line_statements` |
| `IDE2002` | No blank line between two closing braces | `csharp_style_allow_blank_lines_between_consecutive_braces_experimental` |
| `IDE0036` | Modifiers in the default order | `csharp_preferred_modifier_order` |
| `IDE0049` | `int`, not `Int32` (editor only) | `dotnet_style_predefined_type_*` |
| `IDE0054` | `x += 1`, not `x = x + 1` | `dotnet_style_prefer_compound_assignment` |
| `IDE1005` | `Changed?.Invoke(...)` | `csharp_style_conditional_delegate_call` |
| `IDE0083` | `is not`, not `!(x is T)` | `csharp_style_prefer_not_pattern` |
| `IDE0021`, `IDE0022`, `IDE0061` | Block body for constructors, methods and local functions. `IDE0022` is relaxed until 0.1.0: off. Target value: a block body for methods | `csharp_style_expression_bodied_*` |
| `IDE0023`–`IDE0027`, `IDE0053` | `=>` for operators, properties, indexers, accessors and lambdas when it fits on one line (`IDE0053` editor only) | `csharp_style_expression_bodied_*` |
| `IDE1006` | Names: `I` + PascalCase interfaces, `T` + PascalCase type parameters, PascalCase types, members and constants, camelCase private fields, parameters and locals | `dotnet_naming_*` |
| `IDE0051`, `IDE0052` | No private member that no code uses, or that code writes and never reads | |
| `IDE0059` | No value that no code reads. Fix: `_ = Save(order);` | `csharp_style_unused_value_assignment_preference` |
| `IDE0060` | No parameter that no code uses | `dotnet_code_quality_unused_parameters` |
| `IDE0005` | No `using` that the file does not need | `GenerateDocumentationFile` in the root `Directory.Build.props` |
| `IDE0004` | No cast to the type that the value already has | |
| `IDE0079` | No suppression that hides no warning (editor only) | |
| `CA1052`, `CA1012` | A class with only static members is `static`. An abstract class has no public constructor | `dotnet_code_quality.<ID>.api_surface` |
| `CA2000` | Dispose each `IDisposable` object | |
| `CA1849` | No blocking call, such as `.Result`, in an `async` method | |
| `CA1812` | An internal or private class that no code creates | Handlers and the types that dependency injection creates give false warnings until issue #41 is closed |
| `UMP010101` | Characters in a line | `umpire.UMP010101.max` |
| `UMP010102` | Lines in a method | `umpire.UMP010102.max` |
| `UMP010103` | Lines in a file | `umpire.UMP010103.max` |
| `UMP010104` | One top-level type in a file | `umpire.UMP010104.allow_same_name` |
| `UMP010105` | Member order | `umpire.UMP010105.kind_order`, `access_order`, `static_first` |
| `UMP010106` | No comments | |

A rule with a maximum inherits `LimitRule`, which reads the option `max`. In `kind_order` and `access_order`, a name that the list does not have comes after the names of the list.

A rule key wins over a global key, for example `umpire.UMP010201.check_responses` over `umpire.check_responses`. In a config file line, a semicolon starts a comment, so a list option uses commas between its items.

`RuleAnalyzer` skips each rule that is off by itself, because Roslyn skips an analyzer only when all of its IDs are off. If it cannot tell that a rule is off, the rule runs, and Roslyn drops its diagnostics.

| Test | Checks |
|---|---|
| `FormattingRuleTests` | Each VSA rule and its options, on small sources |
| `FormattingBuildTests` | The Umpire.Sdk setup, on the built fixture `tests/Fixtures/Built/Formatting`: each rule gives a warning, the build passes, and the migrations give no warning |
| `RuleOptionTests` | `UMP010001` |
| `RuleValueOwnerTests` | Each option of each rule has a valid default. The package files have no option value. Each key has a rule. The general files have no `UMP` key. No key is a definition option |

The analyzer tests read both analyzer package files, without the severity lines. A test config overrides a value, because each package file has `global_level = -1`.

EF Core writes the files in `Infrastructure/Migrations/`. Umpire.Sdk marks them as generated code (`Umpire.Migrations.globalconfig` in `obj/`), so no rule checks them.

## Architecture: the rule engine

The rule engine binds each type of the app to a part of the VSA definition. Its checks give `UMP020101`, `UMP020102`, `UMP020201`, `UMP020202`, `UMP020301`, `UMP020302` and the warning `UMP020203`. The build hook package `Umpire.Engine.Build` on the host project runs it after each build. A test project does not check the architecture. See `architecture-engine.md` for the definition API, the binding and app rules.

### Counts and matches

The VSA definition has these count and match lines in `Rules` (see "Counts and matches" in `architecture-engine.md`):

| ID | Rule | Line | Code |
|---|---|---|---|
| S1 | A slice has at least one handler or hook | `For<Slice>().Has<Handlers>().AtLeast(1)` | `UMP020301` |
| S2 | The handlers of one slice are of one kind. A hook is a kind | `For<Slice>().Has<Handlers>().OfOneKind()` | `UMP020301` |
| S3a | A request handler handles a request of its own slice | `For<Handlers.Requests>().Under<Feature>().Takes<Contracts.Requests>().InSame<Slice>()` | `UMP020302` |
| S3b | Each request and each response of a feature has exactly one handler | `For<Contracts.Requests>().Under<Feature>().Has<Handlers.Requests>(Depth.Self).Exactly(1)` | `UMP020301` |
| S3c, S3d | The same two lines for the commands | `Handlers.Commands`, `Contracts.Commands` | `UMP020302`, `UMP020301` |
| M1 | A feature module and a plugin module have exactly one root class | `For<Feature>().Has<Module>().Exactly(1)`, the same for `Plugin` | `UMP020301` |

An event has zero or more handlers, so it has no line. A hook takes its command, so S3d counts `Handlers.Commands` with `Depth.Self`: the nested `Handlers.Commands.Hooks` part does not count. A command result follows its command, so it does not count. S3a-d have `Under<Feature>()`, so they do not check a plugin module: a plugin has no slice, and its base commands have no handler. So a plugin request with two handlers, or a plugin command with no handler, gives no finding.

The hooks have their own parts: `Handlers.Commands.Hooks` (`BeforeCommandHook<>`, `AfterCommandHook<,>`) and `Handlers.Events.Hooks` (`BeforeEventHook<>`, `AfterEventHook<>`). A hook has each use of the handlers of its kind. In a slice, the hook parts are siblings of the handler parts in the slice folder. In a plugin module, `Handlers.Commands.Hooks` is a child of `Handlers.Commands` (and the same for the events), with `InParentFolder()`. Reason: a plugin has one folder for each layer, so its hooks are in `Handlers/Commands/`, and only a child part can share that folder. A sibling with `InParentFolder()` would be in `Handlers/`.

`Slices/Shared/` is the part `Slice.Shared`, not a slice, so S1, S2 and S3 do not count it. It has the uses of a slice contract, and the slices and the presenters of its own feature use it.

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

1. If a built-in .NET rule does the check, set its severity in the root `.editorconfig`. This is the style of this repository only: Umpire.Sdk ships no native style, and a consumer sets its own.
2. If one file shows the problem, write a rule class on `KernelRule` or `VsaRule`. Set the next free `Number` of its group, and the defaults of its options.
3. Else change the rules of the VSA definition (`packs/Vsa/Architecture/VsaArchitecture.cs`).
4. Add tests. For an analyzer, use `tests/Rules.Tests/AnalyzerTests.cs`. For the engine, add a case to `tests/Architecture.Tests` with the fixture apps in `tests/Fixtures/Architecture/`.
5. Add the rule to the table in this file.
