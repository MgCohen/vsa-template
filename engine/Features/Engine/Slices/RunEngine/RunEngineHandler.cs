namespace Engine.Slices.RunEngine;

using Blocks.Slices.FindBlocks;
using LoadApp.Domain.Shared;
using LoadApp.Slices.LoadApp;
using Report.Domain.Shared;
using Report.Slices.WriteReport;
using Rules.Slices.CheckRules;
using Steps.Domain.Shared;
using Steps.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Commands;
using Versions.Slices.CheckVersions;

sealed class RunEngineHandler(ICommandSender commands, StepLog log) : CommandHandler<RunEngineCommand, ReportResult> {
    public override async Task<Result<ReportResult>> HandleAsync(Envelope<RunEngineCommand> envelope) {
        var run = envelope.Message;
        log.FailFast = run.FailFast;
        var findings = await Run(run, envelope.Cancellation);
        return await commands.SendAsync(new WriteReportCommand(run.Mode, findings), envelope.Cancellation);
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
        return [.. log.Findings];
    }
}
