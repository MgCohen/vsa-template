using System.Text.Json;
using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Infrastructure;

namespace Viewer.Tests.Export;

public static class NotesFacts
{
    private static readonly Lazy<JsonElement> root = new(() => JsonDocument.Parse(NotesExport.Json).RootElement);

    private static readonly Lazy<Definition> definition = new(() => ArchitectureImport.Read(NotesExport.DefinitionJson));

    private static readonly Lazy<IReadOnlyList<RuleTerm>> terms = new(ReadTerms);

    public static int PartCards => definition.Value.Parts.Count - 1;

    public static int NodeEdges => root.Value.GetProperty("app").GetProperty("dependencies").EnumerateArray()
        .Select(d => new LayerPair(d.GetProperty("from").GetString(), d.GetProperty("to").GetString()))
        .Where(d => d.From != d.To)
        .Distinct()
        .Count();

    public static IReadOnlySet<string> RuleTerms => terms.Value.Select(t => t.Edge).ToHashSet();

    public static IReadOnlySet<string> FilterLabels => terms.Value.Where(t => t.Filters.Length > 0)
        .Select(t => t.Filters).ToHashSet();

    public static IReadOnlyList<string> AllowedRules => Single("Allow");

    public static IReadOnlyList<string> DeniedRules => Single("Deny");

    public static IReadOnlyList<IReadOnlyList<string>> MergedRules => [.. terms.Value
        .GroupBy(t => t.Pair)
        .Where(g => g.Count() == 2 && g.All(t => t.Access == "Allow"))
        .Select(g => (IReadOnlyList<string>)[.. g.Select(t => t.Edge)])];

    private static List<string> Single(string access)
    {
        HashSet<LayerPair> shared = [.. terms.Value.GroupBy(t => t.Pair).Where(g => g.Count() > 1).Select(g => g.Key)];
        return [.. terms.Value
            .GroupBy(t => t.Rule)
            .Where(g => g.Count() == 1)
            .Select(g => g.Single())
            .Where(t => !shared.Contains(t.Pair) && t.Access == access && t.Filters.Length == 0)
            .Select(t => t.Edge)];
    }

    private static List<RuleTerm> ReadTerms()
    {
        return [.. definition.Value.Rules
            .SelectMany(r => r.Froms.SelectMany(f => r.Tos
                .Select(t => new RuleTerm(r.Id, r.Access, new LayerPair(f.Layer, t.Layer), Filters(t)))))
            .Where(t => t.Pair.From != t.Pair.To)];
    }

    private static string Filters(Term term)
    {
        return string.Join(", ", term.Conditions.Select(c => $"{c.Kind}<{c.Layer}>"));
    }

    private sealed record LayerPair(string? From, string? To);

    private sealed record RuleTerm(string Rule, string Access, LayerPair Pair, string Filters)
    {
        public string Edge => $"{Rule}:{Pair.From}>{Pair.To}";
    }
}
