namespace EngineCore.Tests;

using Rules.Domain;
using BuiltApp.Infrastructure;
using Rules.Slices.CheckRules;
using Steps.Domain.Shared;

public sealed class ArchitectureRuleTests {
    [Test]
    public void The_rules_give_the_codes_of_the_architecture_findings() =>
        Assert.That(ArchitectureRule.All.Select(rule => rule.Code), Is.EqualTo(new[] {
            "UMP020101", "UMP020102", "UMP020201", "UMP020202", "UMP020203", "UMP020301", "UMP020302", "UMP020401", "UMP020402",
        }));

    [Test]
    public void Other_code_finds_a_rule_by_its_type() {
        Assert.Multiple(() => {
            Assert.That(ArchitectureRule.Of<DeniedUseRule>().Code, Is.EqualTo("UMP020101"));
            Assert.That(ArchitectureRule.Of<MissingScriptAssembliesRule>().Severity, Is.EqualTo(FindingSeverity.Warning));
        });
    }

    [Test]
    public void The_findings_of_a_rule_have_its_code() {
        var rule = ArchitectureRule.Of<DeniedUseRule>();

        var findings = rule.Check(FixtureApps.Bad, RuleConfig.Empty);

        Assert.That(findings.Select(f => f.Code).Distinct(), Is.EqualTo(new[] { rule.Code }));
    }

    [Test]
    public void A_severity_of_none_in_the_config_turns_a_rule_off() {
        var config = Config("dotnet_diagnostic.UMP020101.severity = none");

        Assert.That(ArchitectureRule.Of<DeniedUseRule>().Check(FixtureApps.Bad, config), Is.Empty);
    }

    [Test]
    public void A_severity_in_the_config_replaces_the_severity_of_the_rule() {
        var config = Config("dotnet_diagnostic.UMP020101.severity = warning");

        var findings = ArchitectureRule.Of<DeniedUseRule>().Check(FixtureApps.Bad, config);

        Assert.That(findings.Select(f => f.Severity).Distinct(), Is.EqualTo(new[] { FindingSeverity.Warning }));
    }

    [Test]
    public void A_rule_value_wins_over_a_global_value() {
        var values = Config("umpire.key = global", "umpire.UMP020101.key = rule").For("UMP020101");

        Assert.That(values.Value("key"), Is.EqualTo("rule"));
    }

    [Test]
    public void The_config_file_has_only_the_keys_above_the_first_section() {
        var values = UmpireConfigFile.Values([
            "# help", "umpire.max = 3 ; a comment", "", "[*.cs]", "umpire.other = 4",
        ]);

        Assert.That(values, Is.EqualTo(new Dictionary<string, string> { ["umpire.max"] = "3" }));
    }

    [Test]
    public void The_config_file_takes_a_colon_as_the_separator() =>
        Assert.That(UmpireConfigFile.Values(["umpire.max: 3"])["umpire.max"], Is.EqualTo("3"));

    [Test]
    public void The_engine_reads_the_nearest_config_file_above_the_block_folder() {
        var root = Directory.CreateTempSubdirectory("umpire-config").FullName;
        try {
            var block = Directory.CreateDirectory(Path.Combine(root, "App", "Block")).FullName;
            File.WriteAllText(Path.Combine(root, "App", UmpireConfigFile.FileName), "dotnet_diagnostic.UMP020101.severity = none");

            var values = new RuleConfig(UmpireConfigFile.Read(block)).For("UMP020101");

            Assert.That(values.Severity(FindingSeverity.Error), Is.Null);
        } finally {
            Directory.Delete(root, recursive: true);
        }
    }

    static RuleConfig Config(params string[] lines) => new(UmpireConfigFile.Values(lines));
}
