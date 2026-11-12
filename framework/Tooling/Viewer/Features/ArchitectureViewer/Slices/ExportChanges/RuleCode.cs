using ArchitectureViewer.Domain.Definitions;
using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Slices.ExportChanges;

internal static class RuleCode
{
    public static IEnumerable<string> Lines(Definition definition, IReadOnlyList<RuleChange> changes)
    {
        IEnumerable<IGrouping<string?, RuleChange>> edited = changes
            .Where(c => c.RuleId is not null)
            .GroupBy(c => c.RuleId);
        foreach (IGrouping<string?, RuleChange> edits in edited)
        {
            foreach (string line in Edited(definition.Rules.First(r => r.Id == edits.Key), [.. edits]))
            {
                yield return line;
            }
        }
        foreach (RuleChange change in changes.Where(c => c.Op == RuleOp.AddRule))
        {
            yield return $"+ {Statement([change.From], [change.To], change.Access)}";
        }
    }

    private static IEnumerable<string> Edited(Rule rule, IReadOnlyList<RuleChange> edits)
    {
        HashSet<(string, string)> touched = [.. edits.Select(c => (c.From.Layer, c.To.Layer))];
        yield return $"- {Statement(rule.Froms, rule.Tos, rule.Access)}";
        IEnumerable<(Term From, Term To)> kept = rule.Froms
            .SelectMany(f => rule.Tos.Where(t => !touched.Contains((f.Layer, t.Layer))).Select(t => (From: f, To: t)));
        foreach (IGrouping<string, IGrouping<Term, (Term From, Term To)>> group in kept.GroupBy(k => k.From)
            .GroupBy(g => string.Join(",", g.Select(k => TermText.Of(k.To)))))
        {
            List<Term> froms = [.. group.Select(g => g.Key)];
            yield return $"+ {Statement(froms, [.. group.First().Select(k => k.To)], rule.Access)}";
        }
        foreach (RuleChange change in edits.Where(c => c.Op == RuleOp.ChangeAccess))
        {
            yield return $"+ {Statement([change.From], [change.To], change.Access)}";
        }
    }

    private static string Statement(IReadOnlyList<Term> froms, IReadOnlyList<Term> tos, string access)
    {
        string verb = access == Access.Deny ? "Denies" : "Uses";
        string from = $"map.For<{froms[0].Layer}>(){Ands(froms)}{Calls(froms[0])}";
        return $"{from}.{verb}<{tos[0].Layer}>(){Ands(tos)}{Calls(tos[0])};";
    }

    private static string Ands(IReadOnlyList<Term> terms)
    {
        return string.Concat(terms.Skip(1).Select(t => $".And<{t.Layer}>()"));
    }

    private static string Calls(Term term)
    {
        return string.Concat(term.Conditions.Select(c => $".{TermText.Of(c)}()"));
    }
}
