using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using VSA.Analyzers;

namespace Rules.Tests;

// The test code compiles against .NET 10 and the VSA layer assemblies. Each test runs the one VsaAnalyzer, so all rules run.
static class Analyzer {
    static readonly ReferenceAssemblies Net10 =
        new("net10.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0"), Path.Combine("ref", "net10.0"));

    public static Task Verify(string source, params DiagnosticResult[] expected) => Verify(source, "", expected);

    // config: lines of a .globalconfig, for the VSA analyzer options.
    public static Task Verify(string source, string config, params DiagnosticResult[] expected) {
        var test = new CSharpAnalyzerTest<VsaAnalyzer, DefaultVerifier> {
            TestCode = "using System;\nusing System.Net.Http;\nusing System.Threading;\nusing System.Threading.Tasks;\n" + source,
            ReferenceAssemblies = Net10,
        };
        Type[] layers = [typeof(VSA.Contracts.Requests.IRequest<>), typeof(VSA.Contracts.Events.IntegrationEvent),
            typeof(VSA.Handlers.Events.IntegrationEventHandler<>), typeof(VSA.Presenters.IBus)];
        foreach (var type in layers) {
            test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(type.Assembly.Location));
        }
        if (config.Length > 0) {
            test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", "is_global = true\n" + config));
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
        public sealed record Items(int Count);
        """);

    [Test]
    public Task Request_class_fails() => Analyzer.Verify($$"""
        public sealed class {|#0:GetItems|} : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record Items(int Count);
        """, Analyzer.Diagnostic(SealedRecordRule.Descriptor, 0, "GetItems", "a request"));

    [Test]
    public Task Response_record_that_is_not_sealed_fails() => Analyzer.Verify($$"""
        public sealed record GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record {|#0:Items|}(int Count);
        """, Analyzer.Diagnostic(SealedRecordRule.Descriptor, 0, "Items", "the response of GetItems"));

    [Test]
    public Task Rule_option_turns_off_the_response_check() => Analyzer.Verify($$"""
        public sealed record GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record Items(int Count);
        """, "vsa.VSA1001.check_responses = false");

    // A global option applies to each VSA rule that reads the key. A rule option wins over it.
    [Test]
    public Task Rule_option_wins_over_the_global_option() => Analyzer.Verify($$"""
        public sealed record GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record {|#0:Items|}(int Count);
        """, "vsa.check_responses = false\nvsa.VSA1001.check_responses = true", Analyzer.Diagnostic(SealedRecordRule.Descriptor, 0, "Items", "the response of GetItems"));

    // Severity none: VsaAnalyzer does not register the rule, so the rule reports nothing.
    [Test]
    public Task Rule_with_severity_none_reports_nothing() => Analyzer.Verify($$"""
        public sealed class GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record Items(int Count);
        """, "dotnet_diagnostic.VSA1001.severity = none");

    // A response from another assembly is not checked here: its own project checks it.
    [Test]
    public Task Response_from_another_assembly_is_not_checked() => Analyzer.Verify($$"""
        public sealed record GetName : VSA.Contracts.Requests.IRequest<string> { {{Route}} }
        """);
}

public sealed class EventHandlerRequestRuleTests {
    const string Types = """
        public sealed record Done : VSA.Contracts.Events.IntegrationEvent;
        public sealed record Ping : VSA.Contracts.Requests.IRequest<Pong> {
            public static HttpMethod Method => HttpMethod.Post;
            public static string Route => "/ping";
        }
        public sealed record Pong;

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

public sealed class VsaAnalyzerTests {
    [Test]
    public void The_analyzer_has_each_rule_of_the_assembly() =>
        Assert.That(new VsaAnalyzer().SupportedDiagnostics.Select(d => d.Id), Is.SupersetOf(new[] { "VSA1001", "VSA1002" }));

    [Test]
    public void Each_rule_has_a_unique_ID() =>
        Assert.That(VsaAnalyzer.Rules.Select(r => r.Id), Is.Unique);
}
