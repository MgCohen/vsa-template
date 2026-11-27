using Tests.Shared;

namespace EngineCore.Tests;

using System.Reflection;
using Operations.Domain.Shared;
using Umpire.CheckModel;
using Umpire.CheckModel.Export;
using Umpire.Config;
using Finding = Operations.Domain.Shared.Finding;

public sealed class ArchitectureRuleTests {
    [Test]
    public void The_rules_give_the_codes_of_the_architecture_findings() =>
        Assert.That(Codes(), Is.EqualTo(new[] {
            "UMP020101", "UMP020102", "UMP020201", "UMP020202", "UMP020204", "UMP020205", "UMP020206",
            "UMP020301", "UMP020302", "UMP020303", "UMP020304", "UMP020401", "UMP020402",
        }));

    [Test]
    public void Other_code_finds_a_rule_by_its_name() {
        Assert.Multiple(() => {
            Assert.That(RuleCodes.DeniedUse, Is.EqualTo("UMP020101"));
        });
    }

    [Test]
    public void The_findings_of_a_rule_have_its_code() {
        var findings = FixtureApps.Bad.Findings().Where(f => f.Message.EndsWith("Remove the use, or change the rule in Rules.", StringComparison.Ordinal));

        Assert.That(findings.Select(f => f.Code).Distinct(), Is.EqualTo(new[] { RuleCodes.DeniedUse }));
    }

    [Test]
    public void A_severity_of_none_in_the_config_turns_a_rule_off() {
        var findings = Bad("umpire.UMP020101.severity = \"none\"");

        Assert.That(findings.Where(f => f.Code == RuleCodes.DeniedUse), Is.Empty);
    }

    [Test]
    public void A_severity_in_the_config_replaces_the_severity_of_the_rule() {
        var findings = Bad("umpire.UMP020101.severity = \"warning\"").Where(f => f.Code == RuleCodes.DeniedUse);

        Assert.That(findings.Select(f => f.Severity).Distinct(), Is.EqualTo(new[] { FindingSeverity.Warning }));
    }

    [Test]
    public void A_severity_of_one_rule_does_not_change_another_rule() {
        var findings = Bad("umpire.UMP020101.severity = \"none\"");

        Assert.That(findings.Where(f => f.Code == RuleCodes.WideExposure).Select(f => f.Severity).Distinct(),
            Is.EqualTo(new[] { FindingSeverity.Error }));
    }

    [Test]
    public void A_severity_of_a_rule_matches_its_key_with_no_case() =>
        Assert.That(Bad("UMPIRE.ump020101.SEVERITY = \"none\"").Where(f => f.Code == RuleCodes.DeniedUse), Is.Empty);

    [Test]
    public void The_engine_reads_the_nearest_config_file_above_the_block_folder() {
        var root = Directory.CreateTempSubdirectory("umpire-config").FullName;
        try {
            var block = Directory.CreateDirectory(Path.Combine(root, "App", "Block")).FullName;
            File.WriteAllText(Path.Combine(root, "App", UmpireConfig.FileName), "umpire.UMP020101.severity = \"none\"");
            var state = StateFolder.At(TempFolder.Create());

            var settings = BlockExport.Of(state, new BlockInput(block, []) { Config = UmpireConfig.Find(block).Values() });

            Assert.That(settings.SeverityOf(RuleCodes.DeniedUse, Severity.Error), Is.Null);
        } finally {
            Directory.Delete(root, recursive: true);
        }
    }

    static IReadOnlyList<string> Codes() => [.. typeof(RuleCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral).Select(f => (string)f.GetValue(null)!).Order(StringComparer.Ordinal)];

    static IReadOnlyList<Finding> Bad(params string[] lines) =>
        FixtureApps.Load(new BadArchitecture(), AppContext.BaseDirectory, FixtureApps.Block("BadApp"),
            config: UmpireConfig.Parse(string.Join("\n", lines)).Values()).Findings();
}
