namespace Rules.Infrastructure;

public sealed record RuleCheck(IReadOnlyList<string> Findings, int Types, string AppRoot);
