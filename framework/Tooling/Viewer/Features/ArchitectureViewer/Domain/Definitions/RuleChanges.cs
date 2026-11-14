using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Definitions;

public static class RuleChanges
{
    public static (string?, string, string) Key(RuleChange change)
    {
        return (change.RuleId, change.From.Layer, change.To.Layer);
    }

    public static IReadOnlyList<Condition> Conditions(RuleChange change)
    {
        return [.. change.From.Conditions, .. change.To.Conditions];
    }
}
