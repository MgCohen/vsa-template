using System.Diagnostics;
using Definition.Slices.LoadDefinition;
using Hello.Slices.SayHello;
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
        var message = envelope.Message;
        var root = Path.GetFullPath(message.Root);
        var bin = Path.GetFullPath(message.Bin);
        var skip = List(message.Skip);
        var off = List(message.Off);

        try {
            var loaded = await Step(commands.SendAsync(new LoadDefinitionCommand(root, bin, skip), ct));
            if (loaded.Definition is null) {
                return await Report(loaded.Findings, "no definition.");
            }
            var rules = await Step(commands.SendAsync(new CheckRulesCommand(loaded.Definition, loaded.Assemblies, root), ct));
            var findings = rules.Findings.ToList();

            if (On("Hello")) {
                findings.AddRange((await Step(commands.SendAsync(new SayHelloCommand(root), ct))).Findings);
            }

            return await Report(findings, $"definition {loaded.Definition.GetType().Name}, {rules.Types} types,");
        } catch (Exception e) when (e is not OperationCanceledException) {
            var stopped = new Finding("VSA3000", FindingSeverity.Error, $"The engine could not run: {e.Message}");
            return await Report([stopped], "stopped.");
        }

        bool On(string plugin) => !off.Contains(plugin, StringComparer.OrdinalIgnoreCase);

        async Task<Result<RunCheckResult>> Report(IReadOnlyList<Finding> findings, string summary) {
            var text = $"{summary} {clock.ElapsedMilliseconds} ms,";
            var report = await Step(commands.SendAsync(new WriteReportCommand(message.Mode, findings, text), ct));
            return new RunCheckResult(report.ExitCode, report.Lines);
        }
    }

    static string[] List(string text) => text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    static async Task<T> Step<T>(Task<Result<T>> send) {
        var result = await send;
        return result.IsSuccess ? result.Value : throw new InvalidOperationException($"{result.Error!.Code}: {result.Error.Message}");
    }
}
