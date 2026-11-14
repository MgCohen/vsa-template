using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Definitions;

public static class RuleEdges
{
    public static IReadOnlyList<RuleEdge> Of(Definition definition, IReadOnlyList<RuleChange> changes)
    {
        PartTree tree = new(definition.Parts);
        ILookup<string, Pair> pairs = definition.Pairs.ToLookup(p => p.Rule);
        Dictionary<(string?, string, string), RuleChange> edits = changes.Where(c => c.RuleId is not null)
            .ToDictionary(RuleChanges.Key);
        IEnumerable<RuleEdge> placed = definition.Rules.SelectMany(r => Placed(tree, r, [.. pairs[r.Id]]))
            .Select(e => Edited(e, edits));
        IEnumerable<RuleEdge> added = changes.Where(c => c.Op == RuleOp.AddRule).Select(Added);
        return [.. placed, .. added];
    }

    private static RuleEdge Edited(RuleEdge edge, Dictionary<(string?, string, string), RuleChange> edits)
    {
        return edits.TryGetValue(edge.Key, out RuleChange? edit)
            ? edge with { Access = edit.Access, Change = edit }
            : edge;
    }

    private static RuleEdge Added(RuleChange change)
    {
        string from = TermText.Of(change.From);
        string to = TermText.Of(change.To);
        string id = $"new:{from}>{to}:{change.FromPart}>{change.ToPart}";
        string text = $"{from} {AccessRules.Verb(change.Access)} {to}";
        return new(id, text, change.From, change.To, change.Access, change.FromPart!, change.ToPart!, change);
    }

    private static IEnumerable<RuleEdge> Placed(PartTree tree, Rule rule, IReadOnlyList<Pair> pairs)
    {
        return from f in rule.Froms
               from t in rule.Tos
               let at = Place(tree, f.Layer, t.Layer, pairs)
               where at is not null
               select new RuleEdge(
                   $"{rule.Id}:{f.Layer}>{t.Layer}",
                   rule.Text,
                   f,
                   t,
                   rule.Access,
                   at.Value.From,
                   at.Value.To,
                   RuleId: rule.Id);
    }

    private static (string From, string To)? Place(PartTree tree, string from, string to, IReadOnlyList<Pair> pairs)
    {
        return tree.ByLayer(from).SelectMany(f => tree.ByLayer(to).Select(t => (From: f.Id, To: t.Id)))
            .Where(c => pairs.Any(p => tree.Within(p.From, c.From) && tree.Within(p.To, c.To)))
            .OrderByDescending(c => tree.Depth(tree.Common(c.From, c.To)))
            .Select(c => ((string, string)?)c)
            .FirstOrDefault();
    }
}
