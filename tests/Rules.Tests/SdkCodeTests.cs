using Tests.Shared;
using CodeCheck.Slices.CheckCode;
using Microsoft.CodeAnalysis;
using UmpireEngine;

namespace Rules.Tests;

public sealed class SdkCodeTests {
    static readonly DiagnosticDescriptor[] Codes = [.. new EngineAnalyzer().SupportedDiagnostics
        .Where(d => d.Id.StartsWith("UMP01", StringComparison.Ordinal))];

    [Test]
    public void The_code_list_of_the_SDK_has_each_analyzer_code_and_no_other() {
        var codes = Codes.Select(d => d.Id).Distinct();

        Assert.That(SdkCodes.All.Where(code => code.StartsWith("UMP01", StringComparison.Ordinal)), Is.EquivalentTo(codes));
        Assert.That(SdkCodes.All, Is.Unique);
    }

    [Test]
    public void Each_analyzer_finding_is_a_warning_by_default() {
        var severities = new EngineAnalyzer().SupportedDiagnostics.Select(d => d.DefaultSeverity);

        Assert.That(severities, Is.All.EqualTo(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning));
    }

    [Test]
    public void Each_analyzer_code_is_an_error_in_this_repository_or_a_listed_warning() {
        var props = System.Xml.Linq.XDocument.Load(Repo.Path("Directory.Build.props"));
        var errors = props.Descendants("WarningsAsErrors").Single().Value.Split(';');
        var warnings = props.Descendants("WarningsNotAsErrors").Single().Value.Split(';');
        var off = CodeRule.All.Where(rule => !rule.IsOnByDefault).Select(rule => rule.Id);
        var codes = Codes.Select(d => d.Id).Distinct();

        Assert.That(codes.Except(off).Except(errors).Except(warnings), Is.Empty);
    }

    [Test]
    public void Each_code_of_the_rules_table_of_the_engine_docs_is_in_the_code_list() {
        var docs = File.ReadAllText(Repo.Path("docs", "architecture-engine.md"));

        var codes = System.Text.RegularExpressions.Regex.Matches(docs, @"(?m)^\| `(UMP0[23]\d{4})` \|").Select(m => m.Groups[1].Value).Distinct();

        Assert.That(codes.Except(SdkCodes.All), Is.Empty);
    }
}
