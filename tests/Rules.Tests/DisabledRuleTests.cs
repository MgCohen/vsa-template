using System.Text.RegularExpressions;
using Tests.Shared;
using UmpireEngine;

namespace Rules.Tests;

public sealed partial class DisabledRuleTests {
    static readonly string Rules = File.ReadAllText(Repo.Path("docs", "architecture-rules.md"));

    [Test]
    public void The_disabled_rules_are_the_rules_of_the_table_in_the_docs() =>
        Assert.That(Disabled(), Is.EquivalentTo(Table()));

    [Test]
    public void Each_disabled_rule_still_has_its_analyzer() {
        var ids = new EngineAnalyzer().SupportedDiagnostics.Select(d => d.Id);

        Assert.That(ids, Is.SupersetOf(Disabled()));
    }

    [Test]
    public void The_section_of_the_disabled_rules_has_the_table() =>
        Assert.That(Section(), Does.Contain("| Code | Class | Why disabled | Issue |"));

    [Test]
    public void Each_row_of_the_table_names_issue_143() {
        var rows = TableRows().ToList();

        Assert.That(rows, Has.All.Contains("#143"));
    }

    static IEnumerable<string> Disabled() =>
        Analyzer.AllRules.Where(rule => !rule.IsOnByDefault).Select(rule => rule.Id);

    static IEnumerable<string> Table() =>
        TableRows().Select(row => RowCode().Match(row).Groups["code"].Value);

    static IEnumerable<string> TableRows() =>
        Section().Split('\n').Where(line => RowCode().IsMatch(line));

    static string Section() {
        var section = Rules[Rules.IndexOf("## Disabled rules", StringComparison.Ordinal)..];
        return section[..section.IndexOf("\n## ", 1, StringComparison.Ordinal)];
    }

    [GeneratedRegex(@"^\| `(?<code>UMP\d{6})` \|")]
    private static partial Regex RowCode();
}
