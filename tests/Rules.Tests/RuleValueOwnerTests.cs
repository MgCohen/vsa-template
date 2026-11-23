using System.Text.RegularExpressions;
using Tests.Shared;
using Umpire.Kernels;
using Umpire.Vsa.Analyzers;

namespace Rules.Tests;

public sealed partial class RuleValueOwnerTests {
    static readonly RuleAnalyzer[] Analyzers = [new KernelAnalyzer(), new VsaAnalyzer()];

    [Test]
    public void Each_option_has_a_valid_default_on_its_rule() {
        var missing = Analyzers.SelectMany(analyzer => analyzer.Rules.SelectMany(rule => rule.Options
            .Where(option => !option.IsValid(null))
            .Select(option => $"{rule.GetType().Name}: {option.Key}")));

        Assert.That(missing, Is.Empty);
    }

    [Test]
    public void The_repository_style_has_no_Umpire_rule_key() {
        var keys = File.ReadAllLines(Repo.Path(".editorconfig")).Where(line => UmpireKey().IsMatch(line));

        Assert.That(keys, Is.Empty);
    }

    [Test]
    public void Umpire_Sdk_ships_no_style_file() =>
        Assert.That(SdkFiles().Where(file => file.EndsWith(".globalconfig", StringComparison.Ordinal)), Is.Empty);

    [Test]
    public void The_general_build_files_name_the_default_pack_in_one_group() {
        var lines = SdkFiles()
            .Where(file => Path.GetExtension(file) is ".props" or ".targets")
            .SelectMany(file => File.ReadAllLines(file)
                .Select((line, index) => new PackLine(Path.GetFileName(file), index, line))
                .Where(line => line.Text.Contains("Umpire.Vsa", StringComparison.Ordinal)))
            .ToList();

        Assert.That(lines.Select(line => line.File).Distinct(), Is.EqualTo(new[] { "Umpire.Sdk.Core.targets" }));
        Assert.That(lines[^1].Index - lines[0].Index, Is.EqualTo(lines.Count - 1), "The pack lines are not one group.");
    }

    [Test]
    public void The_general_build_files_name_no_analyzer_rule() {
        var lines = SdkFiles()
            .SelectMany(file => File.ReadAllLines(file)
                .Where(line => AnalyzerRuleCode().IsMatch(line))
                .Select(line => $"{Path.GetFileName(file)}: {line.Trim()}"));

        Assert.That(lines, Is.Empty);
    }

    [Test]
    public void No_key_is_a_definition_option() {
        var definition = typeof(Umpire.Architecture.PartOptions).GetProperties()
            .Select(property => Plain(property.Name))
            .ToHashSet();

        var clashes = Analyzers.SelectMany(analyzer => analyzer.Rules.SelectMany(rule => rule.Options))
            .Where(option => definition.Contains(Plain(option.Key)));

        Assert.That(clashes, Is.Empty);
    }

    static IEnumerable<string> SdkFiles() =>
        new[] { Repo.Path("tools", "sdk", "Sdk"), Repo.Path("tools", "sdk", "Web", "Sdk") }
            .SelectMany(Directory.EnumerateFiles);

    static string Plain(string name) => name.Replace("_", "", StringComparison.Ordinal).ToUpperInvariant();

    [GeneratedRegex(@"^\s*(umpire\.|dotnet_diagnostic\.UMP)")]
    private static partial Regex UmpireKey();

    [GeneratedRegex(@"UMP01(?!00)\d{4}")]
    private static partial Regex AnalyzerRuleCode();

    sealed record PackLine(string File, int Index, string Text);
}
