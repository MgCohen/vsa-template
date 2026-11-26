using System.Text.RegularExpressions;
using Tests.Shared;
using Umpire.Kernels;
using Umpire.Vsa.Analyzers;

namespace Rules.Tests;

public sealed partial class DisabledRuleTests {
    static readonly string Rules = File.ReadAllText(Repo.Path("docs", "architecture-rules.md"));

    [Test]
    public void The_disabled_rules_are_the_rules_of_the_table_in_the_docs() =>
        Assert.That(Disabled(), Is.EquivalentTo(Table()));

    [Test]
    public void Each_disabled_rule_still_has_its_analyzer() {
        var ids = new KernelAnalyzer().SupportedDiagnostics.Concat(new VsaAnalyzer().SupportedDiagnostics).Select(d => d.Id);

        Assert.That(ids, Is.SupersetOf(Disabled()));
    }

    [Test]
    public void Each_row_of_the_table_names_issue_143() {
        var rows = TableRows().ToList();

        Assert.That(rows, Has.All.Contains("#143"));
    }

    static IEnumerable<string> Disabled() =>
        Analyzer.AllRules.Where(rule => !rule.IsOnByDefault).Select(rule => rule.Id);

    static IEnumerable<string> Table() =>
        TableRows().Select(row => RowCode().Match(row).Groups["code"].Value);

    static IEnumerable<string> TableRows() {
        var section = Rules[Rules.IndexOf("## Disabled rules", StringComparison.Ordinal)..];
        section = section[..section.IndexOf("\n## ", 1, StringComparison.Ordinal)];
        return section.Split('\n').Where(line => RowCode().IsMatch(line));
    }

    [GeneratedRegex(@"^\| `(?<code>UMP\d{6})` \|")]
    private static partial Regex RowCode();
}
