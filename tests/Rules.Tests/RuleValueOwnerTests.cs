using System.Text.RegularExpressions;
using Tests.Shared;
using Umpire.Kernel;
using Umpire.Vsa.Analyzers;

namespace Rules.Tests;

public sealed partial class RuleValueOwnerTests {
    static readonly RuleAnalyzer[] Analyzers = [new KernelAnalyzer(), new VsaAnalyzer()];

    [Test]
    public void Each_option_has_a_value_in_the_file_of_its_package() {
        var missing = Analyzers.SelectMany(analyzer => analyzer.Rules.SelectMany(rule => rule.RequiredOptions
            .Where(option => !option.IsValid(ConfigOf(analyzer).TryValue(Key(rule, option))))
            .Select(option => $"{ConfigOf(analyzer).FileName}: {Key(rule, option)}")));

        Assert.That(missing, Is.Empty);
    }

    [Test]
    public void No_option_is_in_the_file_of_another_package() {
        var misplaced = Analyzers.SelectMany(analyzer => analyzer.Rules.SelectMany(rule => rule.RequiredOptions
            .SelectMany(option => RulesConfig.All
                .Where(config => config != ConfigOf(analyzer) && config.TryValue(Key(rule, option)) is not null)
                .Select(config => $"{config.FileName}: {Key(rule, option)}"))));

        Assert.That(misplaced, Is.Empty);
    }

    [Test]
    public void Each_key_belongs_to_a_rule_of_its_package() {
        var orphans = Analyzers.SelectMany(analyzer => ConfigOf(analyzer).Keys
            .Where(key => !Owns(analyzer, key))
            .Select(key => $"{ConfigOf(analyzer).FileName}: {key}"));

        Assert.That(orphans, Is.Empty);
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
        var definition = typeof(Umpire.Architecture.Options).GetProperties()
            .Select(property => Plain(property.Name))
            .ToHashSet();

        var clashes = RulesConfig.All.SelectMany(config => config.Keys)
            .Where(key => definition.Contains(Plain(key.Split('.')[^1])));

        Assert.That(clashes, Is.Empty);
    }

    static RulesConfig ConfigOf(RuleAnalyzer analyzer) =>
        analyzer is KernelAnalyzer ? RulesConfig.Kernel : RulesConfig.Vsa;

    static string Key(UmpireAnalyzerRule rule, UmpireOption option) => $"umpire.{rule.Id}.{option.Key}";

    static bool Owns(RuleAnalyzer analyzer, string key) =>
        analyzer.Rules.Any(rule => rule.RequiredOptions.Any(option => Key(rule, option) == key))
        || analyzer.Rules.Any(rule => key == $"dotnet_diagnostic.{rule.Id}.severity");

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
