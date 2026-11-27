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
    public void The_Umpire_packages_ship_no_style_file() =>
        Assert.That(BuildFiles().Where(file => file.EndsWith(".globalconfig", StringComparison.Ordinal)), Is.Empty);

    [Test]
    public void The_build_files_of_the_package_Umpire_name_no_pack() {
        var lines = Directory.EnumerateFiles(Repo.Path("tools", "bundle", "Umpire", "build"))
            .SelectMany(file => File.ReadAllLines(file)
                .Where(line => line.Contains("Umpire.Vsa", StringComparison.Ordinal))
                .Select(line => $"{Path.GetFileName(file)}: {line.Trim()}"));

        Assert.That(lines, Is.Empty, "The package of a pack names its definition in its own build props.");
        Assert.That(File.ReadAllText(Repo.Path("tools", "bundle", "Vsa", "build", "Umpire.Vsa.props")),
            Does.Contain("<_UmpirePackDefinition Include=\"Umpire.Vsa\" Package=\"Umpire.Vsa.Definition\" />"));
    }

    [Test]
    public void Only_the_code_list_of_the_general_build_files_names_an_analyzer_rule() {
        var lines = BuildFiles()
            .Where(file => Path.GetFileName(file) != Path.GetFileName(SdkCodes.File))
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

    static IEnumerable<string> BuildFiles() =>
        Directory.EnumerateDirectories(Repo.Path("tools", "bundle"))
            .SelectMany(folder => new[] { Path.Combine(folder, "build"), folder })
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder))
            .Where(file => Path.GetExtension(file) is ".props" or ".targets" or ".globalconfig");

    static string Plain(string name) => name.Replace("_", "", StringComparison.Ordinal).ToUpperInvariant();

    [GeneratedRegex(@"^\s*(umpire\.|dotnet_diagnostic\.UMP)")]
    private static partial Regex UmpireKey();

    [GeneratedRegex(@"UMP01(?!00)\d{4}")]
    private static partial Regex AnalyzerRuleCode();
}
