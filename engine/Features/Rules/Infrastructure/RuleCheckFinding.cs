namespace Rules.Infrastructure;

public sealed record RuleCheckFinding(string Code, bool IsWarning, string? File, int? Line, string Text);
