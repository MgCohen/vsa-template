using System.Text.Json;
using VSA.Architecture;
using VSA.Architecture.Data;
using VSA.Architecture.Vsa;

namespace Architecture.Tests;

public sealed class DefinitionFileTests {
    static readonly Func<ArchitectureDefinition>[] Definitions = [
        () => new VsaDefinition(),
        () => new LayeredDefinition(),
        () => new GoodDefinition(),
        () => new SameConceptDefinition(true),
    ];

    static IEnumerable<TestCaseData> Cases() =>
        Definitions.Select(make => new TestCaseData(make).SetArgDisplayNames(make().GetType().Name));

    [TestCaseSource(nameof(Cases))]
    public void A_definition_from_its_file_has_the_same_tree(Func<ArchitectureDefinition> make) {
        Assert.That(FromFile(make()).Tree(), Is.EqualTo(make().Tree()));
    }

    [TestCaseSource(nameof(Cases))]
    public void A_definition_from_its_file_resolves_the_same_pairs(Func<ArchitectureDefinition> make) {
        Assert.That(Pairs(FromFile(make())), Is.EqualTo(Pairs(make())));
    }

    [TestCaseSource(nameof(Cases))]
    public void A_definition_from_its_file_gives_the_same_report(Func<ArchitectureDefinition> make) {
        Assert.That(FromFile(make()).Report(), Is.EqualTo(make().Report()));
    }

    [TestCaseSource(nameof(Cases))]
    public void The_export_of_a_definition_from_its_file_gives_the_same_file(Func<ArchitectureDefinition> make) {
        var json = DefinitionJson.Write(DefinitionExport.Of(make()));

        Assert.That(DefinitionJson.Write(DefinitionExport.Of(FromFile(make()))), Is.EqualTo(json));
    }

    [Test]
    public void The_bad_app_checked_with_its_definition_file_gives_the_same_findings() {
        var fromFile = FixtureApps.Load(FromFile(new BadDefinition()), AppContext.BaseDirectory, FixtureApps.Root);

        Assert.That(fromFile.Findings().Select(FixtureApps.Line), Is.EqualTo(FixtureApps.Bad.Findings().Select(FixtureApps.Line)));
        Assert.That(fromFile.Findings(), Has.Count.EqualTo(12));
    }

    [Test]
    public void A_file_of_another_version_fails_the_read() {
        var file = DefinitionExport.Of(new VsaDefinition()) with { Version = DefinitionJson.Version + 1 };

        Assert.That(() => DefinitionJson.Read(DefinitionJson.Write(file)),
            Throws.InstanceOf<JsonException>().With.Message.Contains("Export the definition again"));
    }

    [Test]
    public void A_file_with_an_unknown_concept_fails_with_the_name() {
        var file = DefinitionExport.Of(new LayeredDefinition());
        var wrong = file with { Parts = [file.Parts[0] with { Concept = "Missing.Concept, Missing" }, .. file.Parts.Skip(1)] };

        Assert.That(() => Load(wrong).Tree(), Throws.InstanceOf<JsonException>().With.Message.Contains("Missing.Concept"));
    }

    [Test]
    public void The_schema_lists_the_closed_values() {
        var schema = DefinitionJson.Schema().ToJsonString();

        Assert.That(schema, Does.Contain("\"InSame\"").And.Contain("\"InOther\"").And.Contain("\"Under\""));
        Assert.That(schema, Does.Contain("\"Uses\"").And.Contain("\"Denies\""));
        Assert.That(schema, Does.Contain("\"$schema\""));
    }

    static IEnumerable<string> Pairs(ArchitectureDefinition definition) =>
        definition.Resolve().Select(p => $"{p.From.Path} -> {p.To.Path} {p.Permission} {p.Condition} {p.Text} [{p.Source}]");

    static ArchitectureDefinition FromFile(ArchitectureDefinition definition) => Load(DefinitionExport.Of(definition));

    static JsonDefinition Load(DefinitionFile file) {
        var path = Path.Combine(Path.GetTempPath(), $"definition-{Guid.NewGuid():N}.json");
        DefinitionJson.Save(file, path);
        return new JsonDefinition(path);
    }
}
