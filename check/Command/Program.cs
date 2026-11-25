using Umpire.CheckModel;
using Umpire.CheckModel.Command;

const string Usage = "Usage: umpire-check [--root <block folder>] [--state <state folder>] [--mode Error|Warn]";

if (CommandOptions.Of(args) is not { } options) {
    Console.Error.WriteLine(Usage);
    return (int)ExitCode.BadUsage;
}
Report report;
try {
    report = CheckJob.Run(options.Folder(), options.Mode);
} catch (Exception e) {
    report = new Report();
    report.Fail($"{e.GetType().Name}: {e.Message}");
}
if (report.Reason is { } reason) {
    report.Add(new Finding(DataCodes.NoModel, options.ReportMode() == CheckMode.Warn ? Severity.Warning : Severity.Error, $"The check did not run to its end: {reason}"));
}
foreach (var line in report.Lines) {
    Console.WriteLine(line);
}
Console.Error.WriteLine(string.Join(", ", report.Metadata.Select(pair => $"{pair.Key}: {pair.Value}")));
return (int)(!report.Succeeded ? ExitCode.Broken : report.HasFindings ? ExitCode.Findings : ExitCode.Good);
