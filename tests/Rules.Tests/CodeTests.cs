using System.Text.RegularExpressions;
using Tests.Shared;
using Umpire.Kernel;
using Umpire.Vsa.Analyzers;

namespace Rules.Tests;

public sealed partial class CodeTests {
    static readonly string[] CodeFiles = ["*.cs", "*.md", "*.globalconfig", "*.targets", "*.props", "*.template", "umpire-baseline.txt"];
    static readonly string[] SkippedFolders = ["artifacts~", "bin", "obj", ".git", ".claude", "proposals"];

    [Test]
    public void Each_rule_of_the_Kernel_analyzers_has_an_ID_of_block_01_group_00_or_01() {
        var ids = new KernelAnalyzer().SupportedDiagnostics.Select(d => d.Id);

        Assert.That(ids, Has.All.Matches(@"^UMP010[01]\d{2}$"));
    }

    [Test]
    public void Each_rule_of_the_VSA_analyzers_has_an_ID_of_block_01_group_00_or_02() {
        var ids = new VsaAnalyzer().SupportedDiagnostics.Select(d => d.Id);

        Assert.That(ids, Has.All.Matches(@"^UMP010[02]\d{2}$"));
        Assert.That(ids.Where(id => id[3..7] == "0102"), Is.Not.Empty);
    }

    [Test]
    public void Each_rule_number_is_from_1_to_99() {
        var rules = new KernelAnalyzer().Rules.Concat(new VsaAnalyzer().Rules);

        Assert.That(rules.Where(rule => rule.Number is < 1 or > 99).Select(rule => rule.GetType().Name), Is.Empty);
    }

    [Test]
    public void Each_rule_ID_belongs_to_one_rule() {
        var ids = new KernelAnalyzer().SupportedDiagnostics.Concat(new VsaAnalyzer().SupportedDiagnostics)
            .Select(d => d.Id).Where(id => id != "UMP010001").ToList();

        Assert.That(ids, Is.Unique);
    }

    [Test]
    public void Each_analyzer_rule_is_in_the_rules_table_of_the_docs() {
        var docs = File.ReadAllText(Repo.Path("docs", "architecture-rules.md"));
        var ids = new KernelAnalyzer().SupportedDiagnostics.Concat(new VsaAnalyzer().SupportedDiagnostics).Select(d => d.Id);

        Assert.That(ids.Where(id => !docs.Contains($"| `{id}` |", StringComparison.Ordinal)), Is.Empty);
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

    static IEnumerable<string> RepositoryFiles() => CodeFiles
        .SelectMany(pattern => Directory.EnumerateFiles(Repo.Root, pattern, SearchOption.AllDirectories))
        .Where(file => !Path.GetRelativePath(Repo.Root, file).Split(Path.DirectorySeparatorChar).Any(SkippedFolders.Contains));

    [GeneratedRegex(@"(?<![0-9A-Za-z])UMP(\d{4,5}|\d{7,})(?![0-9A-Za-z])")]
    private static partial Regex OldCode();
}
