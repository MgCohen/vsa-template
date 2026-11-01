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

    public static Task Verify(string source, params DiagnosticResult[] expected) {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> {
            TestCode = "using System;\nusing System.Net.Http;\nusing System.Threading;\nusing System.Threading.Tasks;\n" + source,
            ReferenceAssemblies = Net10,
        };
        Type[] layers = [typeof(VSA.Contracts.Requests.IRequest<>), typeof(VSA.Contracts.Events.IntegrationEvent),
            typeof(VSA.Handlers.Events.IntegrationEventHandler<>), typeof(VSA.Presenters.IBus)];
        foreach (var type in layers) {
            test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(type.Assembly.Location));
        }
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync(TestContext.Current.CancellationToken);
    }

    public static DiagnosticResult Diagnostic(int location, params object[] arguments) =>
        new DiagnosticResult(new TAnalyzer().SupportedDiagnostics[0]).WithLocation(location).WithArguments(arguments);
}

public sealed class SealedRecordAnalyzerTests {
    const string Route = "public static HttpMethod Method => HttpMethod.Get; public static string Route => \"/items\";";

    [Fact]
    public Task Sealed_records_pass() => Analyzer<SealedRecordAnalyzer>.Verify($$"""
        public sealed record GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record Items(int Count);
        """);

    [Fact]
    public Task Request_class_fails() => Analyzer<SealedRecordAnalyzer>.Verify($$"""
        public sealed class {|#0:GetItems|} : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record Items(int Count);
        """, Analyzer<SealedRecordAnalyzer>.Diagnostic(0, "GetItems", "a request"));

    [Fact]
    public Task Response_record_that_is_not_sealed_fails() => Analyzer<SealedRecordAnalyzer>.Verify($$"""
        public sealed record GetItems : VSA.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record {|#0:Items|}(int Count);
        """, Analyzer<SealedRecordAnalyzer>.Diagnostic(0, "Items", "the response of GetItems"));

    // A response from another assembly is not checked here: its own project checks it.
    [Fact]
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

    [Fact]
    public Task Event_handler_with_no_request_passes() => Analyzer<EventHandlerRequestAnalyzer>.Verify(Types + """
        sealed class DoneHandler : VSA.Handlers.Events.IntegrationEventHandler<Done> {
            public override Task HandleAsync(Done integrationEvent, CancellationToken ct) => Task.CompletedTask;
        }
        """);

    [Fact]
    public Task Event_handler_with_IBus_fails() => Analyzer<EventHandlerRequestAnalyzer>.Verify(Types + """
        sealed class DoneHandler(VSA.Presenters.IBus {|#0:bus|}) : VSA.Handlers.Events.IntegrationEventHandler<Done> {
            public override Task HandleAsync(Done integrationEvent, CancellationToken ct) => Task.CompletedTask;
        }
        """, Analyzer<EventHandlerRequestAnalyzer>.Diagnostic(0, "DoneHandler", "IBus"));

    [Fact]
    public Task Event_handler_that_makes_a_request_fails() => Analyzer<EventHandlerRequestAnalyzer>.Verify(Types + """
        sealed class DoneHandler : VSA.Handlers.Events.IntegrationEventHandler<Done> {
            public override Task HandleAsync(Done integrationEvent, CancellationToken ct) {
                _ = {|#0:new Ping()|};
                return Task.CompletedTask;
            }
        }
        """, Analyzer<EventHandlerRequestAnalyzer>.Diagnostic(0, "DoneHandler", "Ping"));

    // The rule is only for event handlers. Other classes can send requests.
    [Fact]
    public Task Other_class_that_sends_a_request_passes() => Analyzer<EventHandlerRequestAnalyzer>.Verify(Types + """
        sealed class Sender(VSA.Presenters.IBus bus) {
            public Task<Pong> Send() => bus.Send(new Ping());
        }
        """);
}
