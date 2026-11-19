namespace Rules.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Umpire.Vsa.Analyzers;
using Umpire.Kernels;

sealed class AllRulesTest : CSharpAnalyzerTest<VsaAnalyzer, DefaultVerifier> {
    protected override IEnumerable<Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer> GetDiagnosticAnalyzers() =>
        [new VsaAnalyzer(), new KernelAnalyzer()];
}

static class Analyzer {
    public static readonly IReadOnlyList<UmpireAnalyzerRule> AllRules = [.. new VsaAnalyzer().Rules, .. new KernelAnalyzer().Rules];

    static readonly DiagnosticDescriptor[] Formatting = [new LineLengthRule().Rule, new MethodLengthRule().Rule,
        new FileLengthRule().Rule, new OneTypePerFileRule().Rule, new MemberOrderRule().Rule, new CommentRule().Rule];

    static readonly ReferenceAssemblies Net10 =
        new("net10.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0"), Path.Combine("ref", "net10.0"));

    public static Task Verify(string source, params DiagnosticResult[] expected) => Verify(source, "", expected);

    public static Task Verify(string source, string config, params DiagnosticResult[] expected) =>
        Verify(source, config, null, expected);

    public static Task VerifyIn(string assembly, string source, params DiagnosticResult[] expected) =>
        Verify(source, "", assembly, expected);

    public static Task VerifyIn(string assembly, string source, string config, params DiagnosticResult[] expected) =>
        Verify(source, config, assembly, expected);

    public static Task VerifyIn(string assembly, string source, (string Assembly, string Source) reference, params DiagnosticResult[] expected) =>
        Verify(source, "", assembly, expected, test => {
            test.TestState.AdditionalProjects[reference.Assembly].Sources.Add(reference.Source);
            test.TestState.AdditionalProjectReferences.Add(reference.Assembly);
        });

    public static Task VerifyFormatting(DiagnosticDescriptor rule, string source, string config, params DiagnosticResult[] expected) =>
        Verify(source, config, null, expected, formatting: rule);

    public static Task VerifyFormattingWithoutRulesConfig(DiagnosticDescriptor rule, string source, string config,
        params DiagnosticResult[] expected) =>
        Verify(source, config, null, expected, formatting: rule, rulesConfig: false);

    static Task Verify(string source, string config, string? assembly, DiagnosticResult[] expected,
        Action<AllRulesTest>? configure = null, DiagnosticDescriptor? formatting = null,
        bool rulesConfig = true) {
        var test = new AllRulesTest {
            TestCode = formatting is not null ? source
                : "using System;\nusing System.Net.Http;\nusing System.Threading;\nusing System.Threading.Tasks;\n" + source,
            ReferenceAssemblies = Net10,
        };
        if (assembly is not null) {
            test.SolutionTransforms.Add((solution, project) => solution.WithProjectAssemblyName(project, assembly));
        }
        Type[] layers = [typeof(Umpire.Messages.Contracts.IMessage), typeof(Umpire.Vsa.Contracts.Requests.IRequest<>), typeof(Umpire.Vsa.Contracts.Events.IntegrationEvent),
            typeof(Umpire.Vsa.Handlers.Events.IntegrationEventHandler<>), typeof(Umpire.Vsa.Contracts.Requests.IRequestSender), typeof(Umpire.Vsa.Presenters.Presenter), typeof(Umpire.Kernels.Module), typeof(Umpire.Vsa.Composition.Plugin),
            typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection), typeof(Umpire.Vsa.Handlers.Commands.CommandHandler<,>),
            typeof(Umpire.Messages.Handlers.IHandler), typeof(Umpire.Vsa.Contracts.Commands.ICommand),
            typeof(Umpire.Vsa.Vsa<>), typeof(Umpire.Architecture.Architecture)];
        foreach (var type in layers) {
            test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(type.Assembly.Location));
        }
        var others = formatting is null ? Formatting : Analyzer.AllRules.Select(r => r.Rule).Where(r => r.Id != formatting.Id);
        var off = others.Select(r => $"dotnet_diagnostic.{r.Id}.severity = none\n");
        var globalConfig = "is_global = true\n" + string.Concat(off) + config;
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
        if (rulesConfig) {
            foreach (var rules in RulesConfig.All) {
                test.TestState.AnalyzerConfigFiles.Add(($"/{rules.FileName}", rules.OptionText));
            }
        }
        if (formatting is not null) {
            test.TestBehaviors |= TestBehaviors.SkipSuppressionCheck | TestBehaviors.SkipGeneratedCodeCheck;
        }
        configure?.Invoke(test);
        foreach (var project in test.TestState.AdditionalProjects.Values) {
            project.AnalyzerConfigFiles.Add(($"/{project.Name}/.globalconfig", globalConfig));
            foreach (var rules in RulesConfig.All) {
                project.AnalyzerConfigFiles.Add(($"/{project.Name}/{rules.FileName}", rules.OptionText));
            }
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
        public sealed record GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """);

    [Test]
    public Task Request_class_fails() => Analyzer.Verify($$"""
        public sealed class {|#0:GetItems|} : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """, Analyzer.Diagnostic(new SealedRecordRule().Rule, 0, "GetItems", "a request"));

    [Test]
    public Task Response_record_that_is_not_sealed_fails() => Analyzer.Verify($$"""
        public sealed record GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record {|#0:Items|}(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """, Analyzer.Diagnostic(new SealedRecordRule().Rule, 0, "Items", "the response of GetItems"));

    [Test]
    public Task Rule_option_turns_off_the_response_check() => Analyzer.Verify($$"""
        public sealed record GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """, "umpire.UMP010201.check_responses = false");

    [Test]
    public Task Rule_option_wins_over_the_global_option() => Analyzer.Verify($$"""
        public sealed record GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record {|#0:Items|}(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """, "umpire.check_responses = false\numpire.UMP010201.check_responses = true", Analyzer.Diagnostic(new SealedRecordRule().Rule, 0, "Items", "the response of GetItems"));

    [Test]
    public Task Rule_with_severity_none_reports_nothing() => Analyzer.Verify($$"""
        public sealed class GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """, "dotnet_diagnostic.UMP010201.severity = none");
}

public sealed class EventHandlerRequestRuleTests {
    const string Types = """
        public sealed record Done : Umpire.Vsa.Contracts.Events.IntegrationEvent;
        public sealed record Ping : Umpire.Vsa.Contracts.Requests.IRequest<Pong> {
        }
        public sealed record Pong : Umpire.Vsa.Contracts.Requests.IResponse;

        """;

    [Test]
    public Task Event_handler_with_no_request_passes() => Analyzer.Verify(Types + """
        sealed class DoneHandler : Umpire.Vsa.Handlers.Events.IntegrationEventHandler<Done> {
            public override Task<Umpire.Messages.Contracts.Result<Umpire.Messages.Contracts.Unit>> HandleAsync(Umpire.Messages.Contracts.Envelope<Done> envelope) => null!;
        }
        """);

    [Test]
    public Task Event_handler_with_the_request_sender_fails() => Analyzer.Verify(Types + """
        sealed class DoneHandler(Umpire.Vsa.Contracts.Requests.IRequestSender {|#0:requests|}) : Umpire.Vsa.Handlers.Events.IntegrationEventHandler<Done> {
            public override Task<Umpire.Messages.Contracts.Result<Umpire.Messages.Contracts.Unit>> HandleAsync(Umpire.Messages.Contracts.Envelope<Done> envelope) => null!;
        }
        """, Analyzer.Diagnostic(new EventHandlerRequestRule().Rule, 0, "DoneHandler", "IRequestSender"));

    [Test]
    public Task Event_handler_that_makes_a_request_fails() => Analyzer.Verify(Types + """
        sealed class DoneHandler : Umpire.Vsa.Handlers.Events.IntegrationEventHandler<Done> {
            public override Task<Umpire.Messages.Contracts.Result<Umpire.Messages.Contracts.Unit>> HandleAsync(Umpire.Messages.Contracts.Envelope<Done> envelope) {
                _ = {|#0:new Ping()|};
                return null!;
            }
        }
        """, Analyzer.Diagnostic(new EventHandlerRequestRule().Rule, 0, "DoneHandler", "Ping"));

    [Test]
    public Task Event_handler_with_the_request_base_fails() => Analyzer.Verify(Types + """
        sealed class DoneHandler(Umpire.Vsa.Contracts.Requests.IRequest {|#0:request|}) : Umpire.Vsa.Handlers.Events.IntegrationEventHandler<Done> {
            public override Task<Umpire.Messages.Contracts.Result<Umpire.Messages.Contracts.Unit>> HandleAsync(Umpire.Messages.Contracts.Envelope<Done> envelope) => null!;
        }
        """, Analyzer.Diagnostic(new EventHandlerRequestRule().Rule, 0, "DoneHandler", "IRequest"));

    [Test]
    public Task Other_class_that_sends_a_request_passes() => Analyzer.Verify(Types + """
        sealed class Sender(Umpire.Vsa.Contracts.Requests.IRequestSender requests) {
            public Task<Umpire.Messages.Contracts.Result<Pong>> Send() => requests.SendAsync(new Ping());
        }
        """);
}

public sealed class PresenterPublishRuleTests {
    const string Types = """
        sealed record Done : Umpire.Vsa.Contracts.Events.IntegrationEvent;

        """;

    [Test]
    public Task Presenter_that_shows_an_event_passes() => Analyzer.Verify(Types + """
        sealed class DonePresenter(Umpire.Vsa.Contracts.Requests.IRequestSender requests) : Umpire.Vsa.Presenters.Presenter(requests), Umpire.Vsa.Presenters.IPresenterHandler<Done> {
            public Task<Umpire.Messages.Contracts.Result<Umpire.Messages.Contracts.Unit>> HandleAsync(Umpire.Messages.Contracts.Envelope<Done> envelope) =>
                Task.FromResult<Umpire.Messages.Contracts.Result<Umpire.Messages.Contracts.Unit>>(Umpire.Messages.Contracts.Unit.Value);
        }
        """);

    [Test]
    public Task Presenter_with_a_publisher_fails() => Analyzer.Verify(Types + """
        sealed class DonePresenter(Umpire.Vsa.Contracts.Requests.IRequestSender requests, Umpire.Vsa.Contracts.Events.IIntegrationEventPublisher {|#0:events|}) : Umpire.Vsa.Presenters.Presenter(requests) {
            public Task Finish() => {|#1:events.PublishAsync(new Done(), CancellationToken.None)|};
        }
        """, Analyzer.Diagnostic(new PresenterPublishRule().Rule, 0, "DonePresenter", "IIntegrationEventPublisher"),
            Analyzer.Diagnostic(new PresenterPublishRule().Rule, 1, "DonePresenter", "IIntegrationEventPublisher"));

    [Test]
    public Task Presenter_with_the_live_presenters_fails() => Analyzer.Verify(Types + """
        sealed class DonePresenter(Umpire.Vsa.Contracts.Requests.IRequestSender requests, Umpire.Vsa.Presenters.LivePresenters {|#0:views|}) : Umpire.Vsa.Presenters.Presenter(requests);
        """, Analyzer.Diagnostic(new PresenterPublishRule().Rule, 0, "DonePresenter", "LivePresenters"));

    [Test]
    public Task Other_class_that_publishes_passes() => Analyzer.Verify(Types + """
        sealed class Finisher(Umpire.Vsa.Contracts.Events.IIntegrationEventPublisher events) {
            public Task Finish() => events.PublishAsync(new Done(), CancellationToken.None);
        }
        """);
}

public sealed class ContractTypeRuleTests {
    const string Route = "public static HttpMethod Method => HttpMethod.Get; public static string Route => \"/notes\";";

    [Test]
    public Task Request_response_and_DTO_pass() => Analyzer.VerifyIn("Notes.Contracts.Requests", $$"""
        public sealed record ListNotes : Umpire.Vsa.Contracts.Requests.IRequest<Notes> { {{Route}} }
        public sealed record Notes(System.Collections.Generic.IReadOnlyList<NoteDto> Items) : Umpire.Vsa.Contracts.Requests.IResponse;
        public sealed record NoteDto(Guid Id, Author By);
        public sealed record Author(string Name);
        """);

    [Test]
    public Task Event_and_its_DTO_pass() => Analyzer.VerifyIn("Notes.Contracts.Events", """
        public sealed record NoteCreated(Guid Id, NoteDto Note) : Umpire.Vsa.Contracts.Events.IntegrationEvent;
        public sealed record NoteDto(string Title);
        """);

    [Test]
    public Task Public_helper_in_a_contracts_project_fails() => Analyzer.VerifyIn("Notes.Contracts.Requests", $$"""
        public sealed record ListNotes : Umpire.Vsa.Contracts.Requests.IRequest<Notes> { {{Route}} }
        public sealed record Notes(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        public static class {|#0:TitleRules|} { public static bool IsValid(string title) => title.Length > 0; }
        """, Analyzer.Diagnostic(new ContractTypeRule().Rule, 0, "TitleRules", "Notes.Contracts.Requests"));

    [Test]
    public Task Internal_helper_in_a_contracts_project_passes() => Analyzer.VerifyIn("Notes.Contracts.Requests", $$"""
        public sealed record ListNotes : Umpire.Vsa.Contracts.Requests.IRequest<Notes> { {{Route}} }
        public sealed record Notes(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        static class TitleRules { public static bool IsValid(string title) => title.Length > 0; }
        """);

    const string Command = """
        namespace Umpire.Vsa.Contracts.Commands { interface ICommand<TResult> { } }

        """;

    [Test]
    public Task Command_and_its_result_pass() => Analyzer.VerifyIn("Notes.Contracts.Commands", Command + """
        public sealed record AddNote(string Title) : Umpire.Vsa.Contracts.Commands.ICommand<NoteAdded>;
        public sealed record NoteAdded(Guid Id);
        """);

    [Test]
    public Task Public_helper_in_a_commands_project_fails() => Analyzer.VerifyIn("Notes.Contracts.Commands", Command + """
        public sealed record AddNote(string Title) : Umpire.Vsa.Contracts.Commands.ICommand<NoteAdded>;
        public sealed record NoteAdded(Guid Id);
        public static class {|#0:TitleRules|} { public static bool IsValid(string title) => title.Length > 0; }
        """, Analyzer.Diagnostic(new ContractTypeRule().Rule, 0, "TitleRules", "Notes.Contracts.Commands"));

    [Test]
    public Task Public_helper_in_another_project_passes() => Analyzer.VerifyIn("Notes.Handlers.Requests", """
        public static class TitleRules { public static bool IsValid(string title) => title.Length > 0; }
        """);
}

public sealed class VsaAnalyzerTests {
    [Test]
    public void The_analyzer_has_each_rule_of_the_assembly() =>
        Assert.That(new VsaAnalyzer().SupportedDiagnostics.Select(d => d.Id), Is.SupersetOf(new[] { "UMP010201", "UMP010202", "UMP010203", "UMP010204", "UMP010205", "UMP010206", "UMP010207" }));

    [Test]
    public void Each_rule_has_a_unique_ID() =>
        Assert.That(Analyzer.AllRules.Select(r => r.Id), Is.Unique);
}

public sealed class HookNameRuleTests {
    const string Bell = """
        public sealed record Rang(string Bell) : Umpire.Vsa.Contracts.Events.IntegrationEvent;

        """;

    const string Chime = """
        public override Task AfterAsync(Umpire.Messages.Contracts.IEnvelope<Rang> envelope) => Task.CompletedTask;
        """;

    [Test]
    public Task Event_hook_with_the_suffix_passes() => Analyzer.VerifyIn("Bells.Handlers.Events", Bell + $$"""
        sealed class ChimeHook : Umpire.Vsa.Handlers.Events.AfterEventHook<Rang> { {{Chime}} }
        """);

    [Test]
    public Task Event_hook_without_the_suffix_fails() => Analyzer.VerifyIn("Bells.Handlers.Events", Bell + $$"""
        sealed class {|#0:Chime|} : Umpire.Vsa.Handlers.Events.AfterEventHook<Rang> { {{Chime}} }
        """, Analyzer.Diagnostic(new HookNameRule().Rule, 0, "Chime", "AfterEventHook", "ChimeHook"));

    [Test]
    public Task Class_that_derives_from_a_hook_through_a_base_fails() => Analyzer.VerifyIn("Bells.Handlers.Events", Bell + $$"""
        abstract class BellHook : Umpire.Vsa.Handlers.Events.BeforeEventHook<Rang> { }
        sealed class {|#0:Loud|} : BellHook {
            public override Task<Rang> BeforeAsync(Umpire.Messages.Contracts.IEnvelope<Rang> envelope) => Task.FromResult(envelope.Message);
        }
        """, Analyzer.Diagnostic(new HookNameRule().Rule, 0, "Loud", "BeforeEventHook", "LoudHook"));

    [Test]
    public Task Command_hook_without_the_suffix_fails() => Analyzer.VerifyIn("Bells.Handlers.Commands", """
        public sealed record Ring(int Times) : Umpire.Vsa.Contracts.Commands.ICommand<int>;
        sealed class {|#0:CountRings|} : Umpire.Vsa.Handlers.Commands.AfterCommandHook<Ring, int> {
            public override Task AfterAsync(Umpire.Messages.Contracts.IEnvelope<Ring> envelope, Umpire.Messages.Contracts.Result<int> outcome) =>
                Task.CompletedTask;
        }
        """, Analyzer.Diagnostic(new HookNameRule().Rule, 0, "CountRings", "AfterCommandHook", "CountRingsHook"));
}
