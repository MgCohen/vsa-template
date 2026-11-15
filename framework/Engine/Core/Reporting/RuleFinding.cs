namespace VSA.Architecture.Engine;

public sealed record RuleFinding(string Code, RuleSeverity Severity, string? File, int Line, int Column, string Text);
