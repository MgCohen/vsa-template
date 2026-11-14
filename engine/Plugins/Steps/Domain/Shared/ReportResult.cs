namespace Steps.Domain.Shared;

public sealed record ReportResult(int ExitCode, IReadOnlyList<string> Lines);
