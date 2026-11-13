namespace Rules.Infrastructure;

public sealed record RuleCheck(IReadOnlyList<RuleCheckFinding> Findings, int Types);
