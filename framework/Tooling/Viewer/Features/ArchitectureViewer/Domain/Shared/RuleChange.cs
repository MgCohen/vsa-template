namespace ArchitectureViewer.Domain.Shared;

public sealed record RuleChange(
    RuleOp Op,
    Term From,
    Term To,
    string Access,
    string? RuleId = null,
    string? FromPart = null,
    string? ToPart = null);
