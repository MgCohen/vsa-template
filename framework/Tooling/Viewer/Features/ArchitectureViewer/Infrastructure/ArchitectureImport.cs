using ArchitectureViewer.Domain.Shared;
using VSA.Architecture.Data;

namespace ArchitectureViewer.Infrastructure;

public static class ArchitectureImport
{
    public static Definition Read(string json)
    {
        DefinitionData file = ArchitectureJson.Read(json).Definition;
        return new Definition(
            file.Name,
            [.. file.Parts.Select(p => new Part(p.Id, p.Layer, p.Parent, p.Path, p.Many, p.OfType))],
            [.. file.Rules.Select(r => new Rule(r.Id, r.Text, r.Access, TermsOf(r.From), TermsOf(r.To)))],
            [.. file.Pairs.Select(p => new Pair(p.From, p.To, p.Rule))]);
    }

    private static List<Term> TermsOf(List<TermData> terms)
    {
        return [.. terms.Select(t => new Term(t.Layer, [.. t.Filters.Select(f => new Condition(f.Kind, f.Layer))]))];
    }
}
