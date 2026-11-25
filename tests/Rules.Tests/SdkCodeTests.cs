using Tests.Shared;
using Umpire.Kernels;
using Umpire.Vsa.Analyzers;

namespace Rules.Tests;

public sealed class SdkCodeTests {
    static readonly RuleAnalyzer[] Analyzers = [new KernelAnalyzer(), new VsaAnalyzer()];

    [Test]
    public void The_code_list_of_the_SDK_has_each_analyzer_code_and_no_other() {
        var codes = Analyzers.SelectMany(a => a.SupportedDiagnostics).Select(d => d.Id).Distinct();

        Assert.That(SdkCodes.All.Where(code => code.StartsWith("UMP01", StringComparison.Ordinal)), Is.EquivalentTo(codes));
        Assert.That(SdkCodes.All, Is.Unique);
    }

    [Test]
    public void Each_analyzer_finding_is_a_warning_by_default() {
        var severities = Analyzers.SelectMany(a => a.SupportedDiagnostics).Select(d => d.DefaultSeverity);

        Assert.That(severities, Is.All.EqualTo(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning));
    }

    [Test]
    public void Each_code_of_the_rules_table_of_the_engine_docs_is_in_the_code_list() {
        var docs = File.ReadAllText(Repo.Path("docs", "architecture-engine.md"));

        var codes = System.Text.RegularExpressions.Regex.Matches(docs, @"(?m)^\| `(UMP0[23]\d{4})` \|").Select(m => m.Groups[1].Value).Distinct();

        Assert.That(codes.Except(SdkCodes.All), Is.Empty);
    }
}
