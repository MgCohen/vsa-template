namespace UmpireEngine;

using System.Diagnostics;
using Operations.Contracts.Requests;
using Operations.Domain.Shared;

public static class ConsoleReport {
    public const string Origin = "umpire-engine";
    const string Umpire = "UMP";
    static readonly string BuildFailed = OperationCode.Prefix + "01";

    public static ConsoleOutput Of(JobReport report) => Of(report.Mode, report.Findings);

    public static ConsoleOutput Of(ReportMode mode, IReadOnlyList<Finding> reported) {
        var warnOnly = mode == ReportMode.Warn;
        var findings = reported.Distinct().Select(f => warnOnly && f.Severity == FindingSeverity.Error && !IsOfBuild(f)
            ? f with { Severity = FindingSeverity.Warning }
            : f)
            .OrderBy(f => f.File is not null)
            .ThenBy(f => f.File, StringComparer.Ordinal)
            .ThenBy(f => f.Line ?? 0)
            .ThenBy(f => f.Column ?? 0)
            .ThenBy(f => f.Code, StringComparer.Ordinal)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .ToList();
        var errors = findings.Count(f => f.Severity == FindingSeverity.Error);
        var warnings = findings.Count(f => f.Severity == FindingSeverity.Warning);
        return new ConsoleOutput(errors > 0 ? 1 : 0, [.. findings.Select(Line)], errors, warnings);
    }

    public static int Write(JobReport report) => Write(report.Mode, report.Findings);

    public static int Write(ReportMode mode, IReadOnlyList<Finding> findings) {
        var output = Of(mode, findings);
        foreach (var line in output.Lines) {
            Console.WriteLine(line);
        }
        Console.Error.WriteLine($"{Origin}: {output.Errors} errors, {output.Warnings} warnings, {ElapsedMilliseconds()} ms.");
        return output.ExitCode;
    }

    static bool IsOfBuild(Finding finding) =>
        !finding.Code.StartsWith(Umpire, StringComparison.Ordinal) || finding.Code.StartsWith(BuildFailed, StringComparison.Ordinal);

    static long ElapsedMilliseconds() {
        using var process = Process.GetCurrentProcess();
        return (long)(DateTime.Now - process.StartTime).TotalMilliseconds;
    }

    static string Line(Finding finding) {
        var origin = finding.File is null ? Origin : $"{finding.File}({finding.Line ?? 1},{finding.Column ?? 1})";
        var severity = finding.Severity switch {
            FindingSeverity.Error => "error",
            FindingSeverity.Warning => "warning",
            _ => "message",
        };
        return finding.Severity == FindingSeverity.Info
            ? $"{origin}: {finding.Code}: {finding.Message}"
            : $"{origin}: {severity} {finding.Code}: {finding.Message}";
    }
}
