using ArchitectureViewer.Domain.Shared;
using FilterKind = VSA.Architecture.FilterKind;
using VSA.Architecture.Data;

namespace ArchitectureViewer.Infrastructure;

public static class DefinitionChanges
{
    public static string Apply(string json, Definition definition, IReadOnlyList<RuleChange> changes)
    {
        DefinitionFile source = DefinitionJson.Read(json);
        List<RuleEdit> edits = [.. changes.Select(c => EditOf(definition, c))];
        return DefinitionJson.Write(DefinitionEdit.Apply(source, edits));
    }

    private static RuleEdit EditOf(Definition definition, RuleChange change)
    {
        RuleVerb verb = change.Access == Access.Deny ? RuleVerb.Denies : RuleVerb.Uses;
        if (change.Op == RuleOp.AddRule)
        {
            return new RuleEdit(
                RuleEditKind.Add, change.From.Layer, change.To.Layer, verb,
                FromFilters: Filters(change.From), ToFilters: Filters(change.To));
        }
        string rule = definition.Rules.First(r => r.Id == change.RuleId).Text;
        RuleEditKind kind = change.Op == RuleOp.ChangeAccess ? RuleEditKind.ChangeVerb : RuleEditKind.Remove;
        return new RuleEdit(kind, change.From.Layer, change.To.Layer, verb, rule);
    }

    private static List<NamedFilter> Filters(Term term)
    {
        return [.. term.Conditions.Select(c => new NamedFilter(Enum.Parse<FilterKind>(c.Kind), c.Layer))];
    }
}
