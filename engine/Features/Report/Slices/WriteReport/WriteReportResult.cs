namespace Report.Slices.WriteReport;

public sealed record WriteReportResult(int ExitCode, IReadOnlyList<string> Lines);
