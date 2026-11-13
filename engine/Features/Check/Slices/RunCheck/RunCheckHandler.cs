using System.Diagnostics;
using Definition.Slices.LoadDefinition;
using Report.Domain.Shared;
using Report.Slices.WriteReport;
using Rules.Slices.CheckRules;
using VSA.Contracts;
using VSA.Contracts.Commands;
using VSA.Handlers.Commands;

namespace Check.Slices.RunCheck;

sealed class RunCheckHandler(ICommandSender commands) : CommandHandler<RunCheckCommand, RunCheckResult> {
    public override async Task<Result<RunCheckResult>> HandleAsync(Envelope<RunCheckCommand> envelope) {
        var clock = Stopwatch.StartNew();
        var ct = envelope.Cancellation;
        var root = Path.GetFullPath(envelope.Message.Root);
        var bin = Path.GetFullPath(envelope.Message.Bin);
        var mode = envelope.Message.Mode;

        try {
            var loaded = await Step(commands.SendAsync(new LoadDefinitionCommand(root, bin), ct));
            if (loaded.Definition is null) {
                return await Report(loaded.Findings, "no definition.");
            }
            var rules = await Step(commands.SendAsync(new CheckRulesCommand(loaded.Definition, loaded.Assemblies, root), ct));
            return await Report(rules.Findings, $"definition {loaded.Definition.GetType().Name}, {rules.Types} types,");
        } catch (Exception e) when (e is not OperationCanceledException) {
            return await Report([new Finding("VSA3000", FindingSeverity.Error, $"The engine could not run: {e.Message}")], "stopped.");
        }

        async Task<Result<RunCheckResult>> Report(IReadOnlyList<Finding> findings, string summary) {
            var text = $"{summary} {clock.ElapsedMilliseconds} ms,";
            var report = await Step(commands.SendAsync(new WriteReportCommand(mode, findings, text), ct));
            return new RunCheckResult(report.ExitCode);
        }
    }

    static async Task<T> Step<T>(Task<Result<T>> send) {
        var result = await send;
        return result.IsSuccess ? result.Value : throw new InvalidOperationException($"{result.Error!.Code}: {result.Error.Message}");
    }
}
