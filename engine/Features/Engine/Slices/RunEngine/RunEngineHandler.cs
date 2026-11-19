namespace Engine.Slices.RunEngine;

using Blocks.Domain.Shared;
using Blocks.Slices.FindBlocks;
using Build.Slices.BuildApp;
using Changes.Slices.FindChanges;
using Changes.Slices.WriteStamp;
using Engine.Infrastructure;
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
        var given = envelope.Message;
        var run = given with { Mode = CheckMode.Of(given.Mode, Folder(given)) };
        options.FailFast = run.FailFast;
        var findings = await Run(run, envelope.Cancellation);
        var report = await commands.SendAsync(new WriteReportCommand(run.Mode!.Value, findings), envelope.Cancellation);
        return report.IsSuccess ? new RunEngineResponse(report.Value.ExitCode, report.Value.Lines) : report.Error!;
    }

    async Task<IReadOnlyList<Finding>> Run(RunEngineRequest run, CancellationToken ct) {
        if (run.Stage.Runs<BuildAppCommand>()) {
            var build = await commands.SendAsync(new BuildAppCommand(run.Project, run.Bin), ct);
            if ((!build.IsSuccess || build.Value.Failed) && run.Stage.Runs<FindBlocksCommand>()) {
                log.Skip<FindBlocksCommand, FindBlocksResult>();
                return [.. log.Findings];
            }
        }
        if (run.Stage.Runs<FindBlocksCommand>()) {
            await Check(run, ct);
        }
        return [.. log.Findings];
    }

    async Task Check(RunEngineRequest run, CancellationToken ct) {
        var changes = await commands.SendAsync(new FindChangesCommand(Folder(run), run.Bin, Inputs(run)), ct);
        if (changes.IsSuccess && changes.Value.Same) {
            return;
        }
        var before = log.Findings.Count;
        await Rules(run, ct);
        if (changes.IsSuccess && log.Findings.Skip(before).All(f => f.Severity == FindingSeverity.Info)) {
            await commands.SendAsync(new WriteStampCommand(run.Bin, changes.Value.Hash), ct);
        }
    }

    static string Folder(RunEngineRequest run) => run.Block.Length > 0 ? run.Block : run.Project;

    static string Inputs(RunEngineRequest run) => $"{run.Mode}|{run.Skip}|{run.Block}";

    async Task Rules(RunEngineRequest run, CancellationToken ct) {
        var blocks = await commands.SendAsync(new FindBlocksCommand(run.Project, run.Bin, run.Block), ct);
        if (!blocks.IsSuccess) {
            return;
        }
        var tree = blocks.Value.Blocks;
        var versions = await commands.SendAsync(new CheckVersionsCommand(tree.Current.Folder, run.Bin), ct);
        var app = versions.IsSuccess
            ? await commands.SendAsync(new LoadAppCommand(tree, blocks.Value.Bin, run.Skip), ct)
            : log.Skip<LoadAppCommand, AppInput>();
        if (app.IsSuccess) {
            await commands.SendAsync(new CheckRulesCommand(app.Value), ct);
        } else {
            log.Skip<CheckRulesCommand, CheckRulesResult>();
        }
    }
}
