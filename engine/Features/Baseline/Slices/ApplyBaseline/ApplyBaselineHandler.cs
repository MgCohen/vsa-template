using Baseline.Domain;
using Baseline.Infrastructure;
using Steps.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Baseline.Slices.ApplyBaseline;

sealed class ApplyBaselineHandler : CommandHandler<ApplyBaselineCommand, ApplyBaselineResult> {
    public const string StaleEntry = "VSA3005";
    public const string UnreadableEntry = "VSA3006";
    public const string NotUpdated = "VSA3007";
    const string EngineCodePrefix = "VSA3";
    const string UpdateSwitch = "build with -p:VsaUpdateBaseline=true";

    public override Task<Result<ApplyBaselineResult>> HandleAsync(Envelope<ApplyBaselineCommand> envelope) =>
        Task.FromResult(Apply(envelope.Message));

    static Result<ApplyBaselineResult> Apply(ApplyBaselineCommand command) {
        var root = Path.GetFullPath(command.AppRoot);
        var path = BaselineFile.PathIn(root);
        var errors = command.Findings.Where(IsBaselineError).Distinct().ToList();
        var incomplete = command.Findings.Any(IsIncompleteRun);
        if (command.Update && !incomplete) {
            BaselineFile.Write(path, errors.Select(error => Entry(error, root)));
        }
        var open = new Dictionary<BaselineEntry, Queue<int>>();
        var lines = BaselineFile.Read(path);
        for (var index = 0; index < lines.Count; index++) {
            if (string.IsNullOrWhiteSpace(lines[index])) {
                continue;
            }
            if (BaselineEntry.Parse(lines[index]) is not { } entry) {
                return new Error(UnreadableEntry, $"{path}({index + 1},1): this baseline line does not have "
                    + $"the form CODE|path|message. Fix the line, or {UpdateSwitch}.");
            }
            if (!open.TryGetValue(entry, out var numbers)) {
                open[entry] = numbers = new Queue<int>();
            }
            numbers.Enqueue(index + 1);
        }
        var accepted = errors.Where(error => Consume(open, Entry(error, root))).ToList();
        var stale = open
            .SelectMany(pair => pair.Value.Select(number => Stale(pair.Key, path, number)))
            .OrderBy(finding => finding.Line)
            .ToList();
        IReadOnlyList<Finding> findings = command.Update && incomplete ? [NotUpdatedWarning(path), .. stale] : stale;
        return new ApplyBaselineResult(findings, accepted);
    }

    static bool IsBaselineError(Finding finding) =>
        finding.Severity == FindingSeverity.Error
        && !finding.Code.StartsWith(EngineCodePrefix, StringComparison.Ordinal);

    static bool IsIncompleteRun(Finding finding) =>
        finding.Code.StartsWith(EngineCodePrefix, StringComparison.Ordinal)
        && finding.Severity is FindingSeverity.Error or FindingSeverity.Info;

    static Finding NotUpdatedWarning(string path) => new(
        NotUpdated,
        FindingSeverity.Warning,
        $"{path} was not updated, because a step failed or was skipped, so the run has not found each error. "
            + "Fix the engine error, then build with -p:VsaUpdateBaseline=true again.");

    static bool Consume(Dictionary<BaselineEntry, Queue<int>> open, BaselineEntry entry) =>
        open.TryGetValue(entry, out var numbers) && numbers.TryDequeue(out _);

    static BaselineEntry Entry(Finding finding, string root) =>
        BaselineEntry.Of(finding.Code, finding.File, finding.Message, root);

    static Finding Stale(BaselineEntry entry, string path, int line) => new(
        StaleEntry,
        FindingSeverity.Warning,
        $"the baseline entry {entry.Code} in {(entry.File.Length == 0 ? "the app" : entry.File)} has no error. "
            + $"Remove the line, or {UpdateSwitch}.",
        path,
        line);
}
