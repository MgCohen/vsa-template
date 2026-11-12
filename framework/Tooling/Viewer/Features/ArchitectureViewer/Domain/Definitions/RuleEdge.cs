using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Definitions;

public sealed record RuleEdge(
    string Id,
    string Text,
    Term From,
    Term To,
    string Access,
    string FromPart,
    string ToPart,
    RuleChange? Change = null,
    string? RuleId = null)
{
    public bool Pending => Change is not null;

    public bool Removed => Change?.Op == RuleOp.RemoveRule;

    public (string?, string, string) Key => (RuleId, From.Layer, To.Layer);

    public string? Label => From.Conditions.Count + To.Conditions.Count == 0
        ? null
        : string.Join(", ", From.Conditions.Concat(To.Conditions).Select(TermText.Of));

    public string Shown => Access == Shared.Access.Deny || Label is null ? Access : Shared.Access.Conditional;

    public bool IsSelf => FromPart == ToPart;
}
