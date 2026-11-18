namespace Engine.Slices.RunEngine;

using Blocks.Domain.Shared;
using Blocks.Slices.FindBlocks;
using Build.Domain.Shared;
using Build.Slices.BuildApp;
using Config.Slices.WriteConfig;
using LoadApp.Domain.Shared;
using LoadApp.Slices.LoadApp;
using Report.Slices.WriteReport;
using Rules.Slices.CheckRules;
using Steps.Domain.Shared;
using Steps.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Requests;
using Versions.Slices.CheckVersions;

sealed class RunEngineHandler(ICommandSender commands, StepLog log, RunOptions options) : RequestHandler<RunEngineRequest, RunEngineResponse> {
    public override async Task<Result<RunEngineResponse>> HandleAsync(Envelope<RunEngineRequest> envelope) {
        var run = envelope.Message;
        options.FailFast = run.FailFast;
        var findings = await Run(run, envelope.Cancellation);
        var report = await commands.SendAsync(new WriteReportCommand(run.Mode, findings), envelope.Cancellation);
        return report.IsSuccess ? new RunEngineResponse(report.Value.ExitCode, report.Value.Lines) : report.Error!;
    }

    async Task<IReadOnlyList<Finding>> Run(RunEngineRequest run, CancellationToken ct) {
        if (!await Prepare(run, ct)) {
            log.Skip<FindBlocksCommand, FindBlocksResult>();
            return [.. log.Findings];
        }
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

    async Task<bool> Prepare(RunEngineRequest run, CancellationToken ct) {
        var folder = run.Block.Length > 0 ? run.Block : run.Project;
        var config = await commands.SendAsync(new WriteConfigCommand(folder), ct);
        if (!config.IsSuccess) {
            log.Skip<BuildAppCommand, BuildAppResult>();
            return false;
        }
        if (!run.Build) {
            return true;
        }
        var build = await commands.SendAsync(new BuildAppCommand(run.Project), ct);
        return build.IsSuccess;
    }
}
