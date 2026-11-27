namespace Rules.Tests;

using CodeCheck.Slices.CheckCode;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using UmpireEngine;

sealed class AllRulesTest : CSharpAnalyzerTest<EngineAnalyzer, DefaultVerifier>;

static class Analyzer {
    public static readonly IReadOnlyList<CodeRule> AllRules = CodeRule.All;

    static readonly CodeRule[] Formatting = [new LineLengthRule(), new MethodLengthRule(),
        new FileLengthRule(), new OneTypePerFileRule(), new MemberOrderRule(), new CommentRule()];

    static readonly ReferenceAssemblies Net10 =
        new("net10.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0"), Path.Combine("ref", "net10.0"));

    public static Task Verify(string source, params DiagnosticResult[] expected) => Verify(source, "", expected);

    public static Task Verify(string source, string config, params DiagnosticResult[] expected) =>
        Verify(source, config, expected, null);

    public static Task VerifyFormatting(CodeRule rule, string source, string config, params DiagnosticResult[] expected) =>
        Verify(source, config, expected, rule);

    public static Task VerifyWithGenerated(string source, string generated) =>
        Verify(source, "", [], null, generated);

    static Task Verify(string source, string config, DiagnosticResult[] expected, CodeRule? formatting, string? generated = null) {
        var test = new AllRulesTest {
            TestCode = formatting is not null ? source
                : "using System;\nusing System.Net.Http;\nusing System.Threading;\nusing System.Threading.Tasks;\n" + source,
            ReferenceAssemblies = Net10,
        };
        if (generated is not null) {
            test.TestState.Sources.Add(("Items.g.cs", generated));
        }
        Type[] layers = [typeof(Umpire.Messages.Contracts.IMessage), typeof(Umpire.Vsa.Contracts.Requests.IRequest<>), typeof(Umpire.Vsa.Contracts.Events.IntegrationEvent),
            typeof(Umpire.Vsa.Handlers.Events.IntegrationEventHandler<>), typeof(Umpire.Vsa.Contracts.Requests.IRequestSender), typeof(Umpire.Vsa.Presenters.Presenter), typeof(Umpire.Kernels.Module), typeof(Umpire.Vsa.Composition.Plugin),
            typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection), typeof(Umpire.Vsa.Handlers.Commands.CommandHandler<,>),
            typeof(Umpire.Messages.Contracts.IHandler), typeof(Umpire.Vsa.Contracts.Commands.ICommand),
            typeof(Umpire.Vsa.Definition.VsaArchitecture), typeof(Umpire.Kernels.PackArchitecture), typeof(Umpire.Architecture.Architecture)];
        foreach (var type in layers) {
            test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(type.Assembly.Location));
        }
        var others = formatting is null ? Formatting : Analyzer.AllRules.Where(r => r.Id != formatting.Id);
        var off = others.Select(r => $"dotnet_diagnostic.{r.Id}.severity = none\n");
        var globalConfig = "is_global = true\n" + string.Concat(off) + TestConfig.Native(config);
        var umpireConfig = TestConfig.Umpire(config);
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
        test.TestState.AdditionalFiles.Add(("/.umpireconfig", umpireConfig));
        if (formatting is not null) {
            test.TestBehaviors |= TestBehaviors.SkipSuppressionCheck | TestBehaviors.SkipGeneratedCodeCheck;
        }
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync(TestContext.CurrentContext.CancellationToken);
    }

    public static DiagnosticResult Diagnostic(CodeRule rule, int location, params object[] arguments) =>
        new DiagnosticResult(rule.Id, DiagnosticSeverity.Warning).WithLocation(location).WithMessage(rule.Text(arguments));
}

public sealed class RecordPayloadRuleTests {
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
        """, Analyzer.Diagnostic(new RecordPayloadRule(), 0, "GetItems", "a request"));

    [Test]
    public Task Abstract_request_base_passes() => Analyzer.Verify($$"""
        public abstract record TestRequest<TResponse> : Umpire.Vsa.Contracts.Requests.IRequest<TResponse>
            where TResponse : Umpire.Vsa.Contracts.Requests.IResponse;
        public sealed record GetItems : TestRequest<Items> { {{Route}} }
        public sealed record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """);

    [Test]
    public Task Records_that_are_not_sealed_pass() => Analyzer.Verify($$"""
        public record GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """);

    [Test]
    public Task Request_that_derives_from_a_concrete_request_passes() => Analyzer.Verify($$"""
        public record GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record GetRecentItems(int Days) : GetItems;
        public sealed record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """);

    [Test]
    public Task Response_class_of_a_derived_request_is_reported_one_time() => Analyzer.Verify($$"""
        public record GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record GetRecentItems(int Days) : GetItems;
        public sealed class {|#0:Items|} : Umpire.Vsa.Contracts.Requests.IResponse { }
        """, Analyzer.Diagnostic(new RecordPayloadRule(), 0, "Items", "the response of GetItems"));

    [Test]
    public Task Record_struct_request_passes() => Analyzer.Verify($$"""
        public readonly record struct GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """);

    [Test]
    public Task Struct_request_fails() => Analyzer.Verify($$"""
        public struct {|#0:GetItems|} : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """, Analyzer.Diagnostic(new RecordPayloadRule(), 0, "GetItems", "a request"));

    [Test]
    public Task Concrete_request_of_an_abstract_base_that_is_not_sealed_passes() => Analyzer.Verify($$"""
        public abstract record TestRequest<TResponse> : Umpire.Vsa.Contracts.Requests.IRequest<TResponse>
            where TResponse : Umpire.Vsa.Contracts.Requests.IResponse;
        public record GetItems : TestRequest<Items> { {{Route}} }
        public sealed record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """);

    [Test]
    public Task Response_of_a_request_with_an_abstract_base_is_checked() => Analyzer.Verify($$"""
        public abstract record TestRequest<TResponse> : Umpire.Vsa.Contracts.Requests.IRequest<TResponse>
            where TResponse : Umpire.Vsa.Contracts.Requests.IResponse;
        public sealed record GetItems : TestRequest<Items> { {{Route}} }
        public sealed class {|#0:Items|} : Umpire.Vsa.Contracts.Requests.IResponse { }
        """, Analyzer.Diagnostic(new RecordPayloadRule(), 0, "Items", "the response of GetItems"));

    [Test]
    public Task Response_class_fails() => Analyzer.Verify($$"""
        public sealed record GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed class {|#0:Items|} : Umpire.Vsa.Contracts.Requests.IResponse { }
        """, Analyzer.Diagnostic(new RecordPayloadRule(), 0, "Items", "the response of GetItems"));

    [Test]
    public Task Response_class_in_generated_code_is_not_reported() => Analyzer.VerifyWithGenerated($$"""
        public sealed record GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        """, """
        // <auto-generated/>
        public sealed class Items : Umpire.Vsa.Contracts.Requests.IResponse { }
        """);

    [Test]
    public Task Rule_option_turns_off_the_response_check() => Analyzer.Verify($$"""
        public sealed record GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed class Items : Umpire.Vsa.Contracts.Requests.IResponse { }
        """, "umpire.UMP010201.check_responses = false");

    [Test]
    public Task Rule_option_wins_over_the_global_option() => Analyzer.Verify($$"""
        public sealed record GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed class {|#0:Items|} : Umpire.Vsa.Contracts.Requests.IResponse { }
        """, "umpire.check_responses = false\numpire.UMP010201.check_responses = true", Analyzer.Diagnostic(new RecordPayloadRule(), 0, "Items", "the response of GetItems"));

    [Test]
    public Task Rule_with_severity_none_reports_nothing() => Analyzer.Verify($$"""
        public sealed class GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """, "dotnet_diagnostic.UMP010201.severity = none");

    [Test]
    public Task Umpire_severity_none_turns_off_the_rule() => Analyzer.Verify($$"""
        public sealed class GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """, "umpire.UMP010201.severity = none");

    [Test]
    public Task Umpire_severity_sets_the_severity_of_the_finding() => Analyzer.Verify($$"""
        public sealed record GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed class {|#0:Items|} : Umpire.Vsa.Contracts.Requests.IResponse { }
        """, "umpire.UMP010201.severity = error",
        Analyzer.Diagnostic(new RecordPayloadRule(), 0, "Items", "the response of GetItems")
            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
}

public sealed class EngineAnalyzerTests {
    [Test]
    public void The_analyzer_has_each_rule_of_the_code_check() =>
        Assert.That(new EngineAnalyzer().SupportedDiagnostics.Select(d => d.Id), Is.SupersetOf(Analyzer.AllRules.Select(r => r.Id)));

    [Test]
    public void Each_rule_has_a_unique_ID() =>
        Assert.That(Analyzer.AllRules.Select(r => r.Id), Is.Unique);
}
