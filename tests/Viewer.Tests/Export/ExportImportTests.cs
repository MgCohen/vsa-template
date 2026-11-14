using System.Text.Json;
using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Infrastructure;
using VSA.Architecture.Data;

namespace Viewer.Tests.Export;

public sealed class ExportImportTests
{
    [Test]
    public void The_viewer_reads_each_part_rule_and_pair_of_the_definition_file()
    {
        JsonDefinition definition = new(VSA.Architecture.Data.DefinitionJson.Read(NotesExport.DefinitionJson));
        Definition read = ArchitectureImport.Read(NotesExport.DefinitionJson);
        Assert.That(read.Parts, Has.Count.EqualTo(definition.Root.Walk().Count()).And.Count.GreaterThan(0));
        int rules = definition.Resolve().Select(p => p.Text).Distinct().Count();
        Assert.That(read.Rules, Has.Count.EqualTo(rules).And.Count.GreaterThan(0));
        Assert.That(read.Pairs, Has.Count.EqualTo(definition.Resolve().Count).And.Count.GreaterThan(0));
    }

    [Test]
    public void Each_rule_term_names_a_part_layer()
    {
        Definition read = ArchitectureImport.Read(NotesExport.DefinitionJson);
        HashSet<string> layers = [.. read.Parts.Select(p => p.Layer)];
        List<Term> terms = [.. read.Rules.SelectMany(r => r.Froms.Concat(r.Tos))];
        IEnumerable<string> filters = terms.SelectMany(t => t.Conditions).Select(c => c.Layer);
        List<string> named = [.. terms.Select(t => t.Layer).Concat(filters)];
        Assert.That(named, Is.Not.Empty.And.All.AnyOf([.. layers]));
    }

    [Test]
    public void The_app_graph_has_a_node_for_each_instance_with_types()
    {
        using JsonDocument export = JsonDocument.Parse(NotesExport.Json);
        JsonElement app = export.RootElement.GetProperty("app");
        int withTypes = app.GetProperty("types").EnumerateArray()
            .Select(t => t.GetProperty("instance").GetString()).Distinct().Count();
        LayoutData graph = AppGraph.Of(NotesExport.Json);
        Assert.That(graph.Nodes, Has.Count.EqualTo(withTypes).And.Count.GreaterThan(0));
        Assert.That(graph.Edges, Is.Not.Empty);
        Assert.That(graph.Groups.Count(g => g.Parent is null), Is.EqualTo(1));
    }

    [Test]
    public void A_missing_field_fails_the_import()
    {
        string renamed = NotesExport.DefinitionJson.Replace("\"verb\":", "\"action\":", StringComparison.Ordinal);
        Assert.That(() => ArchitectureImport.Read(renamed), Throws.InstanceOf<JsonException>());
    }
}
