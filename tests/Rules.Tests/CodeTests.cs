using System.Text.RegularExpressions;
using Tests.Shared;
using CodeCheck.Slices.CheckCode;
using UmpireEngine;

namespace Rules.Tests;

public sealed partial class CodeTests {
    static readonly string[] CodeFiles = ["*.cs", "*.md", "*.globalconfig", "*.targets", "*.props", "*.template"];
    static readonly string[] SkippedFolders = ["artifacts~", "bin", "obj", ".git", ".claude", "proposals"];
    static readonly string[] BaseIds = [.. CodeRule.Notices.Select(notice => notice.Id)];
    static readonly string[] RetiredIds = ["UMP010202", "UMP010203", "UMP010204", "UMP010205", "UMP010206", "UMP010207"];

    [Test]
    public void Each_kernel_rule_has_an_ID_of_block_01_group_01() {
        var ids = CodeRule.All.OfType<KernelRule>().Select(rule => rule.Id);

        Assert.That(ids, Has.All.Matches(@"^UMP010[1]\d{2}$"));
    }

    [Test]
    public void Each_VSA_rule_has_an_ID_of_block_01_group_02() {
        var ids = CodeRule.All.OfType<VsaRule>().Select(rule => rule.Id).ToList();

        Assert.That(ids, Has.All.Matches(@"^UMP010[2]\d{2}$"));
        Assert.That(ids.Where(id => id[3..7] == "0102"), Is.Not.Empty);
    }

    [Test]
    public void Each_rule_number_is_from_1_to_99() {
        var rules = CodeRule.All;

        Assert.That(rules.Where(rule => rule.Number is < 1 or > 99).Select(rule => rule.GetType().Name), Is.Empty);
    }

    [Test]
    public void Each_rule_ID_belongs_to_one_rule() {
        var ids = AnalyzerIds().Where(id => !BaseIds.Contains(id)).ToList();

        Assert.That(ids, Is.Unique);
    }

    [Test]
    public void No_rule_has_a_retired_ID() {
        Assert.That(AnalyzerIds().Intersect(RetiredIds), Is.Empty);
    }

    [Test]
    public void Each_analyzer_rule_is_in_the_rules_table_of_the_docs() {
        var docs = File.ReadAllText(Repo.Path("docs", "architecture-rules.md"));
        Assert.That(AnalyzerIds().Where(id => !docs.Contains($"| `{id}` |", StringComparison.Ordinal)), Is.Empty);
    }

    [Test]
    public void Each_Umpire_code_in_the_repository_has_six_digits() {
        var wrong = RepositoryFiles()
            .SelectMany(file => File.ReadLines(file).Select((line, index) => new { file, line, index }))
            .SelectMany(l => OldCode().Matches(l.line).Select(m => new { l.file, l.index, m.Value }))
            .Select(c => $"{Path.GetRelativePath(Repo.Root, c.file)}({c.index + 1}): {c.Value}")
            .ToList();

        Assert.That(wrong, Is.Empty, "Write each Umpire code as UMP and six digits: block, group, case.");
    }

    static IEnumerable<string> AnalyzerIds() => new EngineAnalyzer().SupportedDiagnostics.Select(d => d.Id)
        .Where(id => id.StartsWith("UMP01", StringComparison.Ordinal));

    static IEnumerable<string> RepositoryFiles() => CodeFiles
        .SelectMany(pattern => Directory.EnumerateFiles(Repo.Root, pattern, SearchOption.AllDirectories))
        .Where(file => !Path.GetRelativePath(Repo.Root, file).Split(Path.DirectorySeparatorChar).Any(SkippedFolders.Contains));

    [GeneratedRegex(@"(?<![0-9A-Za-z])UMP(\d{4,5}|\d{7,})(?![0-9A-Za-z])")]
    private static partial Regex OldCode();
}
