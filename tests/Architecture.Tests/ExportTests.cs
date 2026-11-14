using System.Text.Json;
using VSA.Architecture.Data;
using VSA.Architecture.Engine;

namespace Architecture.Tests;

public sealed class ExportTests {
    static readonly Lazy<ArchitectureFile> Bad = new(() => ArchitectureExport.Of(FixtureApps.Bad, "BadApp"));

    [Test]
    public void The_export_reads_back_to_the_same_json() {
        var json = ArchitectureJson.Write(Bad.Value);

        Assert.That(ArchitectureJson.Write(ArchitectureJson.Read(json)), Is.EqualTo(json));
    }

    [Test]
    public void The_export_has_the_denied_uses_and_the_findings_of_the_app() {
        var app = Bad.Value.App;

        Assert.That(app.Dependencies.Select(d => d.Access), Does.Contain("Deny"));
        Assert.That(app.Findings, Is.Not.Empty);
        Assert.That(app.Findings.Select(f => f.Type).Distinct(), Is.SubsetOf(app.Types.Select(t => t.Id)));
    }

    [Test]
    public void Each_use_names_a_rule_of_the_definition() {
        var rules = FixtureApps.Bad.Definition.Resolve().Select(r => r.Text).ToHashSet();
        var named = Bad.Value.App.Dependencies.SelectMany(d => d.Uses).Select(u => u.Rule).OfType<string>().Distinct();

        Assert.That(named, Is.Not.Empty.And.SubsetOf(rules));
    }

    [Test]
    public void A_file_of_another_version_fails_the_read() {
        var json = ArchitectureJson.Write(Bad.Value with { Version = ArchitectureJson.Version + 1 });

        Assert.That(() => ArchitectureJson.Read(json), Throws.InstanceOf<JsonException>().With.Message.Contains("Export it again"));
    }

    [Test]
    public void The_schema_describes_the_app() {
        var properties = ArchitectureJson.Schema()["properties"]!.AsObject().Select(p => p.Key);

        Assert.That(properties, Is.EquivalentTo(new[] { "version", "app" }));
    }
}
