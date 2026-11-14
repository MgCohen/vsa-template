using ArchitectureViewer.Domain.Shared;
using VSA.Architecture.Data;
using DefinitionPart = VSA.Architecture.Part;
using PartRule = VSA.Architecture.PartRule;

namespace ArchitectureViewer.Infrastructure;

public static class ArchitectureImport
{
    public const string DefaultName = "definition";

    public static Definition Read(string json, string name = DefaultName)
    {
        JsonDefinition definition = new(DefinitionJson.Read(json));
        List<DefinitionPart> parts = [.. definition.Root.Walk()];
        Dictionary<DefinitionPart, string> partIds = parts.Select((p, i) => (p, i))
            .ToDictionary(x => x.p, x => $"p{x.i}");
        Dictionary<string, NamedRule> entries = DefinitionRules.Of(definition)
            .GroupBy(r => r.Text).ToDictionary(g => g.Key, g => g.First());
        List<PartRule> pairs = [.. definition.Resolve()];
        List<string> texts = [.. pairs.Select(p => p.Text).Distinct()];
        Dictionary<string, string> ruleIds = texts.Select((t, i) => (t, i)).ToDictionary(x => x.t, x => $"r{x.i}");
        return new Definition(
            name,
            [.. parts.Select(p => PartOf(p, partIds))],
            [.. texts.Select(t => RuleOf(ruleIds[t], pairs.First(p => p.Text == t), entries.GetValueOrDefault(t)))],
            [.. pairs.Select(p => new Pair(partIds[p.From], partIds[p.To], ruleIds[p.Text]))]);
    }

    private static Part PartOf(DefinitionPart part, Dictionary<DefinitionPart, string> ids)
    {
        string? parent = part.Parent is null ? null : ids[part.Parent];
        return new Part(ids[part], part.Name, parent, part.Path, part.IsGroup, [.. part.Types.Select(t => t.Name)]);
    }

    private static Rule RuleOf(string id, PartRule first, NamedRule? entry)
    {
        return new Rule(id, first.Text, first.Permission.ToString(), TermsOf(entry?.From), TermsOf(entry?.To));
    }

    private static List<Term> TermsOf(IReadOnlyList<NamedTerm>? terms)
    {
        return terms is null
            ? []
            : [.. terms.Select(t => new Term(t.Concept, [.. t.Filters.Select(ConditionOf)]))];
    }

    private static Condition ConditionOf(NamedFilter filter)
    {
        return new Condition(filter.Kind.ToString(), filter.Concept);
    }
}
