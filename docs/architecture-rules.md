# VSA checks

The VSA checks ship with the framework and run on the modules of the consumer. Libraries do the work: the .NET analyzers, Roslyn, ArchUnitNET, NUnit and NSubstitute. VSA adds base classes and one architecture definition (`framework/Architecture/Vsa/VsaDefinition.cs`, see `architecture-engine.md`).

## Levels

| Group | Level | Package | IDs |
|---|---|---|---|
| Editor and build | Build | `VSA.Sdk` (`framework/Sdk`) | `VSA0xxx` |
| Editor and build | Analyzers | Built-in .NET analyzers + `VSA.Analyzers` (`framework/Analyzers`) | `CAxxxx`, `IDExxxx`, `VSA1xxx` |
| Tests | Architecture | `VSA.Testing.Architecture` (architecture engine, ArchUnitNET) | `VSA2xxx` |
| Tests | Unit | `VSA.Testing.Unit` (NUnit, NSubstitute) | None |
| Tests | End-to-end | `VSA.Testing.EndToEnd` | Not started |

The editor and the build run the first group in each consumer project. The second group runs in a consumer test project.

## Rules

| ID | Level | Rule |
|---|---|---|
| `VSA0001` | Build | A module project or a plugin module project gets its references only from VSA.Sdk. A `VsaAppReference` of the app folder is accepted (temporary, see below). |
| `VSA0002` | Build | `VsaPluginUses` is only in a plugin layer, and it names other plugin modules that have that layer. |
| `VSA0003` | Build | A project in a `Features` or `Plugins` folder is in a root folder of the app root. |
| `VSA0004` | Build | A module project is the root `<RootFolder>/<Module>/<Module>.csproj` or `<RootFolder>/<Module>/Projects/<Module>.<Layer>.csproj`. |
| `VSA0005` | Build | The type of each slice file selects one project. See `slice-layout.md`. |
| `VSA0006` | Build | No `Compile` item written by hand, no `.cs` file that no project compiles, and no `Slices/` in a plugin module. |
| `VSA0007` | Build | A module project of a Unity app targets `netstandard2.1`, or sets `VsaUnityCopy` to false. |
| `VSA0008` | Build | The .NET test project of a Unity app finds one app folder in `Assets/`, and no module source there uses `UnityEngine`. |
| `VSA1001` | Analyzers | Requests and responses are sealed records. Option: `check_responses`. |
| `VSA1002` | Analyzers | Event handlers do not use a request type or `IBus`. |
| `VSA1003` | Analyzers | A contracts project holds only requests, responses, commands, events and the types that they use. |
| `VSA1004` | Analyzers | Presenters do not use `IIntegrationEventPublisher`. A presenter shows events (`IPresenterEvent`); it does not publish them. |
| `VSA1005` | Analyzers | A `Domain.Shared` type is immutable: no `set` accessor (`init` is permitted), and each field is `readonly` or `const`. Trial (D-37). |
| `VSA1006` | Analyzers | A `Domain.Shared` type holds only data: no method, operator, conversion or constructor body, and no property body that calls a method. Trial (D-37). |
| `VSA1007` | Analyzers | Each base of a `Domain.Shared` type is in a `.Domain.Shared` assembly or in the option `allowed_bases` (empty in `VSA.Rules.globalconfig`: an interface such as `IComparable<T>` needs a method that `VSA1006` does not permit). Trial (D-37). |
| `VSA1000` | Analyzers | Each option of each running VSA rule is in the config, with a valid value. |
| `VSA1008` | Analyzers | A line has at most `max` characters. Formatting, warning. |
| `VSA1009` | Analyzers | A method, constructor, accessor or local function has at most `max` lines. Formatting, warning. |
| `VSA1010` | Analyzers | A file has at most `max` lines. Formatting, warning. |
| `VSA1011` | Analyzers | A file has one top-level type. Option `allow_same_name` permits `Module` and `Module<T>`. Formatting, warning. |
| `VSA1012` | Analyzers | Members are ordered by kind, then by access, then static before instance. Options: `kind_order`, `access_order`, `static_first`. Formatting, warning. |
| `VSA1013` | Analyzers | The code has no comments: no `//`, no `/* */`, no `///` and no `/** */`, also after a directive such as `#pragma`. A directive, for example `#region`, is not a comment. Code in an inactive `#if` is not checked. Formatting, warning. |
| `VSA1014` | Analyzers | The root class of a plugin module inherits `Plugin` or `Plugin<TSettings>`, and the root class of a module inherits `Module` or `Module<TSettings>`. The host also checks it at startup. |
| `VSA2001` | Architecture | Each type uses only the parts that the VSA definition permits. A plugin module is a base, and it uses no module. |
| `VSA2003` | Architecture | Each type exposes another part only within the access of its rule, for example a handler uses `Domain` but does not expose it outside its assembly. |
| `VSA2101` | Architecture | Each type is assignable to the `OfType` of its part, for example a handler in `Handlers.Requests` inherits `RequestHandler<,>`. |
| `VSA2102` | Architecture | Each type of the app is in a folder of a part. |

`framework/Sdk/Sdk/VSA.Layers.targets` gives the project references of each layer, because VSA.Sdk cannot run C# before compile. Keep it the same as the VSA definition. No test compares the two.

## Temporary: `VsaAppReference`

This item is temporary. Issue [#89](https://github.com/MgCohen/vsa-template/issues/89) replaces it: the definition will give the references at build time, and `VsaAppReference` will go.

A module project cannot reference a `VSA.*` package by hand (`VSA0001`). If a module of the app needs a VSA library that is not a layer base, declare it in a `Directory.Build.props` of the app folder:

```xml
<ItemGroup>
  <VsaAppReference Include="VSA.Architecture.Data" Layer="Infrastructure" Path="Architecture/Data" />
</ItemGroup>
```

| Metadata | Use |
|---|---|
| `Include` | The package ID. Package mode uses the version of VSA.Sdk. |
| `Layer` | The layer that gets the reference in each module, as `VsaLayer` gives it, for example `Infrastructure` or `Handlers/Requests`. |
| `Path` | Source mode only: the project folder under `framework/`. |

VSA.Sdk adds the reference with the framework references of that layer, so `VSA0001` accepts it.

## Root folders and plugin modules

VSA.Sdk finds the module of a project from its folder in the app: `<root>/<RootFolder>/<Name>/`. The layer comes from the project name in `Projects/` (see `slice-layout.md`). The app root is the folder of `global.json` (property `VsaAppRoot`). The root folders are in `VsaRootFolders`: `Features` and `Plugins`. A project in another folder gets no rules. `Shared/` is not a root folder: the plugin modules were Shared modules in `Shared/` before, so move each one to `Plugins/`.

A plugin module has the layers of a module and the folder layout of the framework: one folder for each layer, and no slices (see `slice-layout.md`). It is for the base types of the app, as `framework/Core` is the base of each layer:

| From | Gets from VSA.Sdk |
|---|---|
| A module layer | The same layer of each plugin module. A module root gets each plugin root |
| A module event handler (`Handlers/Events`) | Also the events (`Contracts/Events`) of each module and each plugin module |
| A plugin event handler | Also the events of its own plugin module. The `Handlers/Events` of a plugin module in `<VsaPluginUses>` gives the events of that plugin module |
| A module handler (`Handlers/*`) | Also the commands and the `Domain.Shared` of each module and plugin module, and the `Domain` and the `Infrastructure` of each plugin module |
| A module contracts project (`Contracts/*`) | Also the `Domain.Shared` of each module and plugin module |
| A module `Presenters` | Also the `Domain.Shared` of each module and plugin module |
| A module `Domain.Shared` | No other module. A published domain does not depend on another module |
| A presenter (`Presenters`) | The events of its own module only. A view that shows more than one module shows the presenter of each module |
| A plugin layer | The same layer of each plugin module in `<VsaPluginUses>` (names, with `;`). `VSA0002` checks the names |
| A plugin module | No project of a module |

A plugin module uses another plugin module only if it names it. Two plugin modules that name each other are a cycle, and MSBuild stops the build. A project in a `Features` or `Plugins` folder out of the app root fails with `VSA0003`: set `VsaAppRoot` if the app is not in the folder of `global.json`.

The VSA definition denies each use of a feature module by a plugin module (`VSA2001`). `ModuleRoot.IsPlugin` is true when the root class inherits `Plugin`. `VSA1014` makes the root class agree with the root folder at build time.

The host finds the module roots and their layer projects from the types and the assembly names, with no build records:

| Fact | Source |
|---|---|
| Module root | An assembly with a public, top-level class that inherits `Module` or `Plugin` |
| Plugin module | The root class inherits `Plugin` |
| Layer projects | Each assembly named `<Module>.<Layer>` that loads, for each layer in `ModuleRoot.LayerNames` |

A test project such as `Notes.Tests` is not a layer project, because `Tests` is not a layer name. A nested or internal class that inherits `Module`, for example in a test, does not make a module root.

The host configures the plugin modules first, then the other modules. In each group the order is by name, not by `VsaPluginUses`. The host gets the plugin roots through the module roots, so an app with no module does not load its plugin modules.

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

## Rule files

| File | Holds |
|---|---|
| `VSA.globalconfig` | The built-in .NET rules: editor and code style options, severities |
| `VSA.Rules.globalconfig` | The values of the VSA rules. No rule has a default in code |
| `VSA.Rules.props` | The MSBuild setup: both files, `EnforceCodeStyleInBuild`, the formatting rules as warnings |

`Sdk.props` imports `VSA.Rules.props` for each VSA.Sdk project. The root `Directory.Build.props` imports it for the framework and its tests, so this repository follows the same rules. `Directory.Build.targets` gives the framework projects the VSA analyzer with `VsaFrameworkCode = true`: then only the rules about any code run (`IsModuleRule` is false), not the module rules. `VSA.Analyzers` cannot check itself.

A rule lists its options in `RequiredOptions`. At the start of a compilation, `VsaAnalyzer` checks each one in the global config. If an option is missing or not valid, it reports `VSA1000` (error), and that rule does not run. A consumer changes a value in its own `.editorconfig`.

## Formatting rules

The formatting rules are warnings, so a build does not fail on them (`WarningsNotAsErrors` in `VSA.Rules.props`).

| Rule | Checks | Option |
|---|---|---|
| `IDE0055` | Each brace on its own line (Allman) | `csharp_new_line_before_open_brace` |
| `IDE0011` | Braces on each `if`, `for`, `while` | `csharp_prefer_braces` |
| `IDE0161` | File-scoped namespace | `csharp_style_namespace_declarations` |
| `IDE2000` | No two blank lines in a row | `dotnet_style_allow_multiple_blank_lines_experimental` |
| `IDE0008` | Explicit type, except where the line shows the type | `csharp_style_var_*` |
| `IDE0040` | Access modifier on each member, except in an interface | `dotnet_style_require_accessibility_modifiers` |
| `IDE0055` | One statement on each line | `csharp_preserve_single_line_statements` |
| `IDE2002` | No blank line between two closing braces | `csharp_style_allow_blank_lines_between_consecutive_braces_experimental` |
| `IDE0065` | `using` outside the namespace | `csharp_using_directive_placement` |
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
| `IDE0005` | No `using` that the file does not need | `GenerateDocumentationFile` in `VSA.Rules.props` |
| `IDE0004` | No cast to the type that the value already has | |
| `IDE0079` | No suppression that hides no warning (editor only) | |
| `CA1052`, `CA1012` | A class with only static members is `static`. An abstract class has no public constructor | `dotnet_code_quality.<ID>.api_surface` |
| `CA2000` | Dispose each `IDisposable` object | |
| `CA1849` | No blocking call, such as `.Result`, in an `async` method | |
| `CA1812` | An internal or private class that no code creates | A type with a reflection base is not reported. Handlers and the types that dependency injection creates give false warnings until issue #41 is closed |
| `VSA1008` | Characters in a line | `vsa.VSA1008.max` |
| `VSA1009` | Lines in a method | `vsa.VSA1009.max` |
| `VSA1010` | Lines in a file | `vsa.VSA1010.max` |
| `VSA1011` | One top-level type in a file | `vsa.VSA1011.allow_same_name` |
| `VSA1012` | Member order | `vsa.VSA1012.kind_order`, `access_order`, `static_first` |
| `VSA1013` | No comments | |

A rule with a maximum inherits `LimitRule`, which reads the option `max`. In `kind_order` and `access_order`, a name that the list does not have comes after the names of the list.

| Test | Checks |
|---|---|
| `FormattingRuleTests` | Each VSA rule and its options, on small sources |
| `FormattingBuildTests` | The VSA.Sdk setup, on the built fixture `tests/Fixtures/Built/Formatting`: each rule gives a warning, the build passes, and the migrations give no warning |
| `RuleOptionTests` | `VSA1000`, and each option of each rule is in `VSA.Rules.globalconfig` |

The analyzer tests read the real `VSA.Rules.globalconfig`. A test config overrides a value, because the rules file has `global_level = -1`.

EF Core writes the files in `Infrastructure/Migrations/`. VSA.Sdk marks them as generated code (`VSA.Migrations.globalconfig` in `obj/`), so no rule checks them.

## Reflection bases: `ReflectionBaseSuppressor`

Reflection can call a private member that no code calls, for example a method that a base class finds by its name. It can also set a field that no code writes. The rules for unused code then give false warnings.

Put `[ReflectionBase]` (package `VSA.Analyzers.Markers`, namespace `VSA.Analyzers.Markers`) on the base class or the interface. `ReflectionBaseSuppressor`, a Roslyn `DiagnosticSuppressor` in VSA.Analyzers, then hides the rules in the table below on each type that derives from it or implements it. The base can be indirect or generic, and an interface can inherit the marked interface. A nested type that derives from the base is also covered.

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
| `VSA9001` | `IDE0051`: a private member that no code uses |
| `VSA9002` | `IDE0052`: a private member that no code reads |
| `VSA9003` | `CS0649`: a field that no code writes |
| `VSA9004` | `CS0169`: a field that no code uses |
| `VSA9005` | `IDE0060`: a parameter that no code uses |
| `VSA9006` | `CA1822`: a member that can be static. Reflection that uses `BindingFlags.Instance` cannot find a static member |
| `VSA9007` | `CA1812`: an internal class that no code creates. `Activator` creates it |

A suppressor can hide a warning that `TreatWarningsAsErrors` makes an error, for example CS0649.

Each layer references `VSA.Analyzers.Markers`. It is a separate package because VSA.Analyzers is private (`PrivateAssets="all"`): a DLL in it does not reach the host at runtime.

## Architecture: `ArchitectureTests<TDefinition>`

The architecture engine binds each type of the app to a part of the VSA definition. Four native ArchUnitNET tests give `VSA2001`, `VSA2003`, `VSA2101` and `VSA2102`. A consumer test project references the host and `VSA.Testing.Architecture`, and it writes no test class: the build adds `ArchitectureTests` to `obj/.../VsaArchitectureTests.g.cs` (`framework/Testing/Architecture/build/`). See `architecture-engine.md` for the definition API, the binding and app rules.

To stop the generated class, set `<VsaArchitectureTests>false</VsaArchitectureTests>` in the test project. `Architecture.Tests` does this, because it gives its own definitions. In this repository, `Directory.Build.targets` imports the same target for each project with a `ProjectReference` to `VSA.Testing.Architecture`.

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

1. If a built-in .NET rule does the check, set its severity in `framework/Sdk/Sdk/VSA.globalconfig`.
2. If one file shows the problem, write a rule class that inherits `VsaAnalyzerRule`. Use the next free `VSA1xxx` ID, and add it to `AnalyzerReleases.Unshipped.md`.
3. Else change the rules of the VSA definition (`framework/Architecture/Vsa/VsaDefinition.cs`). If the layers change, also change `VSA.Layers.targets`.
4. Add tests. For a build rule (`VSA0xxx`), add a case to `tests/Rules.Tests/BuildRuleTests.cs`: it builds the built fixture `tests/Fixtures/Built/Shop` from the packed packages. For an analyzer, use `tests/Rules.Tests/AnalyzerTests.cs`. For the engine, add a case to `tests/Architecture.Tests` with the fixture apps in `tests/Fixtures/Architecture/`.
5. Add the rule to the table in this file.
