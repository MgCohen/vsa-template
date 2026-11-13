namespace VSA.Testing.Architecture;

public sealed record RuleEngineRun(int ExitCode, IReadOnlyList<string> Lines, string Summary) {
    public IReadOnlyList<string> Errors => [.. Lines.Where(line => line.Contains(": error ", StringComparison.Ordinal))];

    public string Report => string.Join(Environment.NewLine, [.. Lines, Summary]);
}
