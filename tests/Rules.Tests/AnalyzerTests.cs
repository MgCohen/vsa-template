using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using VSA.Analyzers;

namespace Rules.Tests;

// The test code compiles against .NET 10 and the VSA layer assemblies.
static class Analyzer<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new() {
    static readonly ReferenceAssemblies Net10 =
        new("net10.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0"), Path.Combine("ref", "net10.0"));

    public static Task Verify(string source, params DiagnosticResult[] expected) => Verify(source, "", expected);

    // config: lines of a .globalconfig, for the VSA analyzer options.
    public static Task Verify(string source, string config, params DiagnosticResult[] expected) {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> {
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

    public static DiagnosticResult Diagnostic(int location, params object[] arguments) =>
        new DiagnosticResult(new TAnalyzer().SupportedDiagnostics[0]).WithLocation(location).WithArguments(arguments);
}

public sealed class SealedRecordAnalyzerTests {
    const string Route = "public static HttpMethod Method => HttpMethod.Get; public static string Route => \"/items\";";

    [Test]
    public Task Sealed_records_pass() => Analyzer<SealedRecordAnalyzer>.Verify($$"""
        public sealed record GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record Items(int Count);
        """);

    [Test]
    public Task Request_class_fails() => Analyzer<SealedRecordAnalyzer>.Verify($$"""
        public sealed class {|#0:GetItems|} : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record Items(int Count);
        """, Analyzer<SealedRecordAnalyzer>.Diagnostic(0, "GetItems", "a request"));

    [Test]
    public Task Response_record_that_is_not_sealed_fails() => Analyzer<SealedRecordAnalyzer>.Verify($$"""
        public sealed record GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record {|#0:Items|}(int Count);
        """, Analyzer<SealedRecordAnalyzer>.Diagnostic(0, "Items", "the response of GetItems"));

    [Test]
    public Task Rule_option_turns_off_the_response_check() => Analyzer<SealedRecordAnalyzer>.Verify($$"""
        public sealed record GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record Items(int Count);
        """, "vsa.VSA1001.check_responses = false");

    // A global option applies to each VSA rule that reads the key. A rule option wins over it.
    [Test]
    public Task Rule_option_wins_over_the_global_option() => Analyzer<SealedRecordAnalyzer>.Verify($$"""
        public sealed record GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record {|#0:Items|}(int Count);
        """, "vsa.check_responses = false\nvsa.VSA1001.check_responses = true", Analyzer<SealedRecordAnalyzer>.Diagnostic(0, "Items", "the response of GetItems"));

    // A response from another assembly is not checked here: its own project checks it.
    [Test]
    public Task Response_from_another_assembly_is_not_checked() => Analyzer<SealedRecordAnalyzer>.Verify($$"""
        public sealed record GetName : VSA.Contracts.Requests.IRequest<string> { {{Route}} }
        """);
}

public sealed class EventHandlerRequestAnalyzerTests {
    const string Types = """
        public sealed record Done : VSA.Contracts.Events.IntegrationEvent;
        public sealed record Ping : VSA.Contracts.Requests.IRequest<Pong> {
            public static HttpMethod Method => HttpMethod.Post;
            public static string Route => "/ping";
        }
        public sealed record Pong;

        """;

    [Test]
    public Task Event_handler_with_no_request_passes() => Analyzer<EventHandlerRequestAnalyzer>.Verify(Types + """
        sealed class DoneHandler : VSA.Handlers.Events.IntegrationEventHandler<Done> {
            public override Task HandleAsync(Done integrationEvent, CancellationToken ct) => Task.CompletedTask;
        }
        """);

    [Test]
    public Task Event_handler_with_IBus_fails() => Analyzer<EventHandlerRequestAnalyzer>.Verify(Types + """
        sealed class DoneHandler(VSA.Presenters.IBus {|#0:bus|}) : VSA.Handlers.Events.IntegrationEventHandler<Done> {
            public override Task HandleAsync(Done integrationEvent, CancellationToken ct) => Task.CompletedTask;
        }
        """, Analyzer<EventHandlerRequestAnalyzer>.Diagnostic(0, "DoneHandler", "IBus"));

    [Test]
    public Task Event_handler_that_makes_a_request_fails() => Analyzer<EventHandlerRequestAnalyzer>.Verify(Types + """
        sealed class DoneHandler : VSA.Handlers.Events.IntegrationEventHandler<Done> {
            public override Task HandleAsync(Done integrationEvent, CancellationToken ct) {
                _ = {|#0:new Ping()|};
                return Task.CompletedTask;
            }
        }
        """, Analyzer<EventHandlerRequestAnalyzer>.Diagnostic(0, "DoneHandler", "Ping"));

    // The rule is only for event handlers. Other classes can send requests.
    [Test]
    public Task Other_class_that_sends_a_request_passes() => Analyzer<EventHandlerRequestAnalyzer>.Verify(Types + """
        sealed class Sender(VSA.Presenters.IBus bus) {
            public Task<Pong> Send() => bus.Send(new Ping());
        }
        """);
}
