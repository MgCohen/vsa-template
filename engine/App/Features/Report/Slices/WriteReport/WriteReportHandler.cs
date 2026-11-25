namespace Report.Slices.WriteReport;

using System.Diagnostics;
using Report.Domain.Shared;
using Steps.Domain.Shared;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class WriteReportHandler : CommandHandler<WriteReportCommand, ReportResult> {
    const string Origin = "umpire-engine";
    const string Umpire = "UMP";
    static readonly string BuildFailed = StepCode.Prefix + "01";

    public override Task<Result<ReportResult>> HandleAsync(IEnvelope<WriteReportCommand> envelope) {
        var command = envelope.Message;
        var warnOnly = command.Mode == ReportMode.Warn;
        var findings = command.Findings.Distinct().Select(f => warnOnly && f.Severity == FindingSeverity.Error && !IsOfBuild(f)
            ? f with { Severity = FindingSeverity.Warning }
            : f)
            .OrderBy(f => f.File is not null)
            .ThenBy(f => f.File, StringComparer.Ordinal)
            .ThenBy(f => f.Line ?? 0)
            .ThenBy(f => f.Column ?? 0)
            .ThenBy(f => f.Code, StringComparer.Ordinal)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .ToList();
        var lines = findings.Select(Line).ToList();
        foreach (var line in lines) {
            Console.WriteLine(line);
        }
        var errors = findings.Count(f => f.Severity == FindingSeverity.Error);
        var warnings = findings.Count(f => f.Severity == FindingSeverity.Warning);
        Console.Error.WriteLine($"{Origin}: {errors} errors, {warnings} warnings, {ElapsedMilliseconds()} ms.");
        return Task.FromResult<Result<ReportResult>>(new ReportResult(errors > 0 ? 1 : 0, lines));
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
