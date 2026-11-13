using System.Diagnostics;
using Steps.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Report.Slices.WriteReport;

sealed class WriteReportHandler : CommandHandler<WriteReportCommand, ReportResult> {
    const string Origin = "vsa-engine";

    public override Task<Result<ReportResult>> HandleAsync(Envelope<WriteReportCommand> envelope) {
        var command = envelope.Message;
        var warnOnly = string.Equals(command.Mode, "Warn", StringComparison.OrdinalIgnoreCase);
        var findings = command.Findings.Distinct().Select(f => warnOnly && f.Severity == FindingSeverity.Error
            ? f with { Severity = FindingSeverity.Warning }
            : f).ToList();
        var lines = findings.Select(Line).ToList();
        foreach (var line in lines) {
            Console.WriteLine(line);
        }
        var errors = findings.Count(f => f.Severity == FindingSeverity.Error);
        var warnings = findings.Count(f => f.Severity == FindingSeverity.Warning);
        Console.Error.WriteLine($"{Origin}: {errors} errors, {warnings} warnings, {ElapsedMilliseconds()} ms.");
        return Task.FromResult<Result<ReportResult>>(new ReportResult(errors > 0 ? 1 : 0, lines));
    }

    static long ElapsedMilliseconds() {
        using var process = Process.GetCurrentProcess();
        return (long)(DateTime.Now - process.StartTime).TotalMilliseconds;
    }

    static string Line(Finding finding) {
        var origin = finding.File is null ? Origin : $"{finding.File}({finding.Line ?? 1},1)";
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
