namespace Report.Domain.Shared;

public sealed record ReportResult(int ExitCode, IReadOnlyList<string> Lines);
