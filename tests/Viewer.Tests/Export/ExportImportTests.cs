using System.Text.Json;
using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Infrastructure;

namespace Viewer.Tests.Export;

public sealed class ExportImportTests
{
    [Test]
    public void The_viewer_reads_each_part_rule_and_pair_of_the_export()
    {
        using JsonDocument export = JsonDocument.Parse(NotesExport.Json);
        JsonElement definition = export.RootElement.GetProperty("definition");
        Definition read = ArchitectureImport.Read(NotesExport.Json);
        int parts = definition.GetProperty("parts").GetArrayLength();
        Assert.That(read.Parts, Has.Count.EqualTo(parts).And.Count.GreaterThan(0));
        int rules = definition.GetProperty("rules").GetArrayLength();
        Assert.That(read.Rules, Has.Count.EqualTo(rules).And.Count.GreaterThan(0));
        int pairs = definition.GetProperty("pairs").GetArrayLength();
        Assert.That(read.Pairs, Has.Count.EqualTo(pairs).And.Count.GreaterThan(0));
    }

    [Test]
    public void Each_rule_term_names_a_part_layer()
    {
        Definition read = ArchitectureImport.Read(NotesExport.Json);
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
        string renamed = NotesExport.Json.Replace("\"from\":", "\"source\":", StringComparison.Ordinal);
        Assert.That(() => ArchitectureImport.Read(renamed), Throws.InstanceOf<JsonException>());
    }
}
