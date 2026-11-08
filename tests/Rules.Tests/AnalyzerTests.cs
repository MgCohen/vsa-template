using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using VSA.Analyzers;

namespace Rules.Tests;

// The test code compiles against .NET 10 and the VSA layer assemblies. Each test runs the one VsaAnalyzer, so all rules run,
// except the formatting rules. A VerifyFormatting test runs only its one rule.
static class Analyzer {
    static readonly DiagnosticDescriptor[] Formatting = [LineLengthRule.Descriptor, MethodLengthRule.Descriptor,
        FileLengthRule.Descriptor, OneTypePerFileRule.Descriptor, MemberOrderRule.Descriptor, CommentRule.Descriptor];

    static readonly ReferenceAssemblies Net10 =
        new("net10.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0"), Path.Combine("ref", "net10.0"));

    public static Task Verify(string source, params DiagnosticResult[] expected) => Verify(source, "", expected);

    // config: lines of a .globalconfig, for the VSA analyzer options.
    public static Task Verify(string source, string config, params DiagnosticResult[] expected) =>
        Verify(source, config, null, expected);

    // assembly: the assembly name of the test code, for the rules that depend on the layer, for example Notes.Contracts.Requests.
    public static Task VerifyIn(string assembly, string source, params DiagnosticResult[] expected) =>
        Verify(source, "", assembly, expected);

    public static Task VerifyIn(string assembly, string source, string config, params DiagnosticResult[] expected) =>
        Verify(source, config, assembly, expected);

    // reference: a second project that the test code references. Its assembly name is its project name.
    public static Task VerifyIn(string assembly, string source, (string Assembly, string Source) reference, params DiagnosticResult[] expected) =>
        Verify(source, "", assembly, expected, test => {
            test.TestState.AdditionalProjects[reference.Assembly].Sources.Add(reference.Source);
            test.TestState.AdditionalProjectReferences.Add(reference.Assembly);
        });

    // rule: the one formatting rule that the test checks. The test code gets no usings.
    public static Task VerifyFormatting(DiagnosticDescriptor rule, string source, string config, params DiagnosticResult[] expected) =>
        Verify(source, config, null, expected, formatting: rule);

    // The same, but without VSA.Rules.globalconfig: config gives all options of the rule, or a test of VSA1000 leaves one out.
    public static Task VerifyFormattingWithoutRulesConfig(DiagnosticDescriptor rule, string source, string config,
        params DiagnosticResult[] expected) =>
        Verify(source, config, null, expected, formatting: rule, rulesConfig: false);

    static Task Verify(string source, string config, string? assembly, DiagnosticResult[] expected,
        Action<CSharpAnalyzerTest<VsaAnalyzer, DefaultVerifier>>? configure = null, DiagnosticDescriptor? formatting = null,
        bool rulesConfig = true) {
        var test = new CSharpAnalyzerTest<VsaAnalyzer, DefaultVerifier> {
            TestCode = formatting is not null ? source
                : "using System;\nusing System.Net.Http;\nusing System.Threading;\nusing System.Threading.Tasks;\n" + source,
            ReferenceAssemblies = Net10,
        };
        if (assembly is not null) {
            test.SolutionTransforms.Add((solution, project) => solution.WithProjectAssemblyName(project, assembly));
        }
        Type[] layers = [typeof(VSA.Contracts.IMessage), typeof(VSA.Contracts.Requests.IRequest<>), typeof(VSA.Contracts.Events.IntegrationEvent),
            typeof(VSA.Handlers.Events.IntegrationEventHandler<>), typeof(VSA.Presenters.IBus), typeof(VSA.Module.Module)];
        foreach (var type in layers) {
            test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(type.Assembly.Location));
        }
        // A formatting test turns off each other VSA rule. Another test turns off the formatting rules.
        var others = formatting is null ? Formatting : VsaAnalyzer.Rules.Select(r => r.Rule).Where(r => r.Id != formatting.Id);
        var off = others.Select(r => $"dotnet_diagnostic.{r.Id}.severity = none\n");
        var globalConfig = "is_global = true\n" + string.Concat(off) + config;
        // The test config has the default global_level, so it wins over VSA.Rules.globalconfig (global_level = -1).
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
        if (rulesConfig) {
            test.TestState.AnalyzerConfigFiles.Add(("/VSA.Rules.globalconfig", RulesConfig.Text));
        }
        if (formatting is not null) {
            // The library also checks the code with added lines (#pragma, a generated header). A line or file rule sees those lines.
            test.TestBehaviors |= TestBehaviors.SkipSuppressionCheck | TestBehaviors.SkipGeneratedCodeCheck;
        }
        configure?.Invoke(test);
        // A referenced project gets the same config, so the formatting rules are off there too.
        foreach (var project in test.TestState.AdditionalProjects.Values) {
            project.AnalyzerConfigFiles.Add(($"/{project.Name}/.globalconfig", globalConfig));
            project.AnalyzerConfigFiles.Add(($"/{project.Name}/VSA.Rules.globalconfig", RulesConfig.Text));
        }
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync(TestContext.CurrentContext.CancellationToken);
    }

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor rule, int location, params object[] arguments) =>
        new DiagnosticResult(rule).WithLocation(location).WithArguments(arguments);
}

public sealed class SealedRecordRuleTests {
    const string Route = "public static HttpMethod Method => HttpMethod.Get; public static string Route => \"/items\";";

    [Test]
    public Task Sealed_records_pass() => Analyzer.Verify($$"""
        public sealed record GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record Items(int Count) : VSA.Contracts.Requests.IResponse;
        """);

    [Test]
    public Task Request_class_fails() => Analyzer.Verify($$"""
        public sealed class {|#0:GetItems|} : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record Items(int Count) : VSA.Contracts.Requests.IResponse;
        """, Analyzer.Diagnostic(SealedRecordRule.Descriptor, 0, "GetItems", "a request"));

    [Test]
    public Task Response_record_that_is_not_sealed_fails() => Analyzer.Verify($$"""
        public sealed record GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record {|#0:Items|}(int Count) : VSA.Contracts.Requests.IResponse;
        """, Analyzer.Diagnostic(SealedRecordRule.Descriptor, 0, "Items", "the response of GetItems"));

    [Test]
    public Task Rule_option_turns_off_the_response_check() => Analyzer.Verify($$"""
        public sealed record GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record Items(int Count) : VSA.Contracts.Requests.IResponse;
        """, "vsa.VSA1001.check_responses = false");

    // A global option applies to each VSA rule that reads the key. A rule option wins over it.
    [Test]
    public Task Rule_option_wins_over_the_global_option() => Analyzer.Verify($$"""
        public sealed record GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record {|#0:Items|}(int Count) : VSA.Contracts.Requests.IResponse;
        """, "vsa.check_responses = false\nvsa.VSA1001.check_responses = true", Analyzer.Diagnostic(SealedRecordRule.Descriptor, 0, "Items", "the response of GetItems"));

    // Severity none: VsaAnalyzer does not register the rule, so the rule reports nothing.
    [Test]
    public Task Rule_with_severity_none_reports_nothing() => Analyzer.Verify($$"""
        public sealed class GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record Items(int Count) : VSA.Contracts.Requests.IResponse;
        """, "dotnet_diagnostic.VSA1001.severity = none");
}

public sealed class EventHandlerRequestRuleTests {
    const string Types = """
        public sealed record Done : VSA.Contracts.Events.IntegrationEvent;
        public sealed record Ping : VSA.Contracts.Requests.IRequest<Pong> {
            public static HttpMethod Method => HttpMethod.Post;
            public static string Route => "/ping";
        }
        public sealed record Pong : VSA.Contracts.Requests.IResponse;

        """;

    [Test]
    public Task Event_handler_with_no_request_passes() => Analyzer.Verify(Types + """
        sealed class DoneHandler : VSA.Handlers.Events.IntegrationEventHandler<Done> {
            public override Task HandleAsync(Done integrationEvent, CancellationToken ct) => Task.CompletedTask;
        }
        """);

    [Test]
    public Task Event_handler_with_IBus_fails() => Analyzer.Verify(Types + """
        sealed class DoneHandler(VSA.Presenters.IBus {|#0:bus|}) : VSA.Handlers.Events.IntegrationEventHandler<Done> {
            public override Task HandleAsync(Done integrationEvent, CancellationToken ct) => Task.CompletedTask;
        }
        """, Analyzer.Diagnostic(EventHandlerRequestRule.Descriptor, 0, "DoneHandler", "IBus"));

    [Test]
    public Task Event_handler_that_makes_a_request_fails() => Analyzer.Verify(Types + """
        sealed class DoneHandler : VSA.Handlers.Events.IntegrationEventHandler<Done> {
            public override Task HandleAsync(Done integrationEvent, CancellationToken ct) {
                _ = {|#0:new Ping()|};
                return Task.CompletedTask;
            }
        }
        """, Analyzer.Diagnostic(EventHandlerRequestRule.Descriptor, 0, "DoneHandler", "Ping"));

    // The rule is only for event handlers. Other classes can send requests.
    [Test]
    public Task Other_class_that_sends_a_request_passes() => Analyzer.Verify(Types + """
        sealed class Sender(VSA.Presenters.IBus bus) {
            public Task<Pong> Send() => bus.Send(new Ping());
        }
        """);
}

public sealed class PresenterPublishRuleTests {
    const string Types = """
        sealed record Done : VSA.Contracts.Events.IntegrationEvent;

        """;

    [Test]
    public Task Presenter_that_shows_an_event_passes() => Analyzer.Verify(Types + """
        sealed class DonePresenter(VSA.Presenters.IBus bus) : VSA.Presenters.Presenter(bus), VSA.Presenters.IPresenterEvent<Done> {
            public Task OnEventAsync(Done integrationEvent, CancellationToken ct) => Task.CompletedTask;
        }
        """);

    [Test]
    public Task Presenter_with_a_publisher_fails() => Analyzer.Verify(Types + """
        sealed class DonePresenter(VSA.Presenters.IBus bus, VSA.Contracts.Events.IIntegrationEventPublisher {|#0:events|}) : VSA.Presenters.Presenter(bus) {
            public Task Finish() => {|#1:events.PublishAsync(new Done(), CancellationToken.None)|};
        }
        """, Analyzer.Diagnostic(PresenterPublishRule.Descriptor, 0, "DonePresenter", "IIntegrationEventPublisher"),
            Analyzer.Diagnostic(PresenterPublishRule.Descriptor, 1, "DonePresenter", "IIntegrationEventPublisher"));

    [Test]
    public Task Presenter_with_the_live_presenters_fails() => Analyzer.Verify(Types + """
        sealed class DonePresenter(VSA.Presenters.IBus bus, VSA.Presenters.LivePresenters {|#0:views|}) : VSA.Presenters.Presenter(bus);
        """, Analyzer.Diagnostic(PresenterPublishRule.Descriptor, 0, "DonePresenter", "LivePresenters"));

    // The rule is only for presenters. Request handlers publish events.
    [Test]
    public Task Other_class_that_publishes_passes() => Analyzer.Verify(Types + """
        sealed class Finisher(VSA.Contracts.Events.IIntegrationEventPublisher events) {
            public Task Finish() => events.PublishAsync(new Done(), CancellationToken.None);
        }
        """);
}

public sealed class ContractTypeRuleTests {
    const string Route = "public static HttpMethod Method => HttpMethod.Get; public static string Route => \"/notes\";";

    [Test]
    public Task Request_response_and_DTO_pass() => Analyzer.VerifyIn("Notes.Contracts.Requests", $$"""
        public sealed record ListNotes : VSA.Contracts.Requests.IRequest<Notes> { {{Route}} }
        public sealed record Notes(System.Collections.Generic.IReadOnlyList<NoteDto> Items) : VSA.Contracts.Requests.IResponse;
        public sealed record NoteDto(Guid Id, Author By);
        public sealed record Author(string Name);
        """);

    [Test]
    public Task Event_and_its_DTO_pass() => Analyzer.VerifyIn("Notes.Contracts.Events", """
        public sealed record NoteCreated(Guid Id, NoteDto Note) : VSA.Contracts.Events.IntegrationEvent;
        public sealed record NoteDto(string Title);
        """);

    [Test]
    public Task Public_helper_in_a_contracts_project_fails() => Analyzer.VerifyIn("Notes.Contracts.Requests", $$"""
        public sealed record ListNotes : VSA.Contracts.Requests.IRequest<Notes> { {{Route}} }
        public sealed record Notes(int Count) : VSA.Contracts.Requests.IResponse;
        public static class {|#0:TitleRules|} { public static bool IsValid(string title) => title.Length > 0; }
        """, Analyzer.Diagnostic(ContractTypeRule.Descriptor, 0, "TitleRules", "Notes.Contracts.Requests"));

    [Test]
    public Task Internal_helper_in_a_contracts_project_passes() => Analyzer.VerifyIn("Notes.Contracts.Requests", $$"""
        public sealed record ListNotes : VSA.Contracts.Requests.IRequest<Notes> { {{Route}} }
        public sealed record Notes(int Count) : VSA.Contracts.Requests.IResponse;
        static class TitleRules { public static bool IsValid(string title) => title.Length > 0; }
        """);

    // The test code declares the command marker, because the layer assembly is not referenced here.
    const string Command = """
        namespace VSA.Contracts.Commands { interface ICommand<TResult> { } }

        """;

    [Test]
    public Task Command_and_its_result_pass() => Analyzer.VerifyIn("Notes.Contracts.Commands", Command + """
        public sealed record AddNote(string Title) : VSA.Contracts.Commands.ICommand<NoteAdded>;
        public sealed record NoteAdded(Guid Id);
        """);

    [Test]
    public Task Public_helper_in_a_commands_project_fails() => Analyzer.VerifyIn("Notes.Contracts.Commands", Command + """
        public sealed record AddNote(string Title) : VSA.Contracts.Commands.ICommand<NoteAdded>;
        public sealed record NoteAdded(Guid Id);
        public static class {|#0:TitleRules|} { public static bool IsValid(string title) => title.Length > 0; }
        """, Analyzer.Diagnostic(ContractTypeRule.Descriptor, 0, "TitleRules", "Notes.Contracts.Commands"));

    // The rule is only for the contracts projects. A handlers project can have public types.
    [Test]
    public Task Public_helper_in_another_project_passes() => Analyzer.VerifyIn("Notes.Handlers.Requests", """
        public static class TitleRules { public static bool IsValid(string title) => title.Length > 0; }
        """);
}

public sealed class VsaAnalyzerTests {
    [Test]
    public void The_analyzer_has_each_rule_of_the_assembly() =>
        Assert.That(new VsaAnalyzer().SupportedDiagnostics.Select(d => d.Id), Is.SupersetOf(new[] { "VSA1001", "VSA1002", "VSA1003", "VSA1004", "VSA1005", "VSA1006", "VSA1007" }));

    [Test]
    public void Each_rule_has_a_unique_ID() =>
        Assert.That(VsaAnalyzer.Rules.Select(r => r.Id), Is.Unique);
}

public sealed class PluginRootRuleTests {
    const string Configure = "public override void Configure(VSA.Module.ModuleBuilder module) { }";

    static string In(string rootFolder) => $"build_property.VsaRootFolder = {rootFolder}\n";

    [Test]
    public Task Plugin_root_that_inherits_Plugin_passes() => Analyzer.VerifyIn("Game", $$"""
        public sealed class GamePlugin : VSA.Module.Plugin { {{Configure}} }
        """, In("Plugins"));

    [Test]
    public Task Module_root_that_inherits_Module_passes() => Analyzer.VerifyIn("Shop", $$"""
        public sealed class ShopModule : VSA.Module.Module { {{Configure}} }
        """, In("Modules"));

    [Test]
    public Task Plugin_root_that_inherits_Module_fails() => Analyzer.VerifyIn("Game", $$"""
        public sealed class {|#0:GameModule|} : VSA.Module.Module { {{Configure}} }
        """, In("Plugins"),
        Analyzer.Diagnostic(PluginRootRule.Descriptor, 0, "GameModule", "plugin module", "Game", "Module", "Plugin or Plugin<TSettings>"));

    [Test]
    public Task Module_root_that_inherits_Plugin_fails() => Analyzer.VerifyIn("Shop", $$"""
        public sealed class {|#0:ShopPlugin|} : VSA.Module.Plugin { {{Configure}} }
        """, In("Modules"),
        Analyzer.Diagnostic(PluginRootRule.Descriptor, 0, "ShopPlugin", "module", "Shop", "Plugin", "Module or Module<TSettings>, or move the module to Plugins/"));

    [Test]
    public Task Code_out_of_a_root_folder_passes() => Analyzer.Verify($$"""
        public sealed class ToolPlugin : VSA.Module.Plugin { {{Configure}} }
        """);
}
