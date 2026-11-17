namespace Engine.Slices.RunEngine;

using Baseline.Slices.ApplyBaseline;
using Blocks.Slices.FindBlocks;
using LoadApp.Domain.Shared;
using LoadApp.Slices.LoadApp;
using Report.Slices.WriteReport;
using Rules.Slices.CheckRules;
using Steps.Domain.Shared;
using Steps.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Commands;
using Versions.Slices.CheckVersions;

sealed class RunEngineHandler(ICommandSender commands, StepLog log, RunOptions options) : CommandHandler<RunEngineCommand, RunEngineResult> {
    public override async Task<Result<RunEngineResult>> HandleAsync(Envelope<RunEngineCommand> envelope) {
        var run = envelope.Message;
        options.FailFast = run.FailFast;
        var findings = await Run(run, envelope.Cancellation);
        var report = await commands.SendAsync(new WriteReportCommand(run.Mode, findings), envelope.Cancellation);
        return report.IsSuccess ? new RunEngineResult(report.Value.ExitCode, report.Value.Lines) : report.Error!;
    }

    async Task<IReadOnlyList<Finding>> Run(RunEngineCommand run, CancellationToken ct) {
        var blocks = await commands.SendAsync(new FindBlocksCommand(run.Project, run.Bin, run.Block), ct);
        if (!blocks.IsSuccess) {
            return [.. log.Findings];
        }
        var tree = blocks.Value.Blocks;
        var block = tree.Current.Folder;
        var versions = await commands.SendAsync(new CheckVersionsCommand(block, run.Bin), ct);
        var app = versions.IsSuccess
            ? await commands.SendAsync(new LoadAppCommand(tree, blocks.Value.Bin, run.Skip), ct)
            : log.Skip<LoadAppCommand, AppInput>();
        if (app.IsSuccess) {
            await commands.SendAsync(new CheckRulesCommand(app.Value), ct);
        } else {
            log.Skip<CheckRulesCommand, CheckRulesResult>();
        }
        var found = log.Findings.ToList();
        var baseline = await commands.SendAsync(new ApplyBaselineCommand(block, run.UpdateBaseline, found), ct);
        return [.. log.Findings.Except(baseline.IsSuccess ? baseline.Value.Accepted : [])];
    }
}
