namespace Engine.Slices.RunEngine;

using Baseline.Slices.ApplyBaseline;
using Blocks.Slices.FindBlocks;
using LoadApp.Domain.Shared;
using LoadApp.Slices.LoadApp;
using LoadTemplates.Slices.LoadTemplates;
using PlanScaffold.Slices.PlanScaffold;
using Report.Domain.Shared;
using Report.Slices.WriteReport;
using Rules.Slices.CheckRules;
using Scaffold.Domain.Shared;
using Steps.Domain.Shared;
using Steps.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Commands;
using Versions.Slices.CheckVersions;
using WriteScaffold.Slices.WriteScaffold;

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
        var block = blocks.Value.Blocks.Current.Folder;
        var version = await commands.SendAsync(new CheckVersionsCommand(block, run.Bin), ct);
        var app = await commands.SendAsync(new LoadAppCommand(blocks.Value.Blocks, run.Bin, run.Skip, version.Value), ct);
        if (ScaffoldInput.From(run.ScaffoldKind, run.ScaffoldName, run.ScaffoldGroups) is { } input) {
            return await Scaffold(run, app.Value, block, input, ct);
        }
        await commands.SendAsync(new CheckRulesCommand(app.Value), ct);
        var found = log.Findings.ToList();
        var baseline = await commands.SendAsync(new ApplyBaselineCommand(block, run.UpdateBaseline, found), ct);
        return [.. log.Findings.Except(baseline.IsSuccess ? baseline.Value.Accepted : [])];
    }

    async Task<IReadOnlyList<Finding>> Scaffold(
        RunEngineCommand run, AppInput app, string block, ScaffoldInput input, CancellationToken ct) {
        var templates = await commands.SendAsync(
            new LoadTemplatesCommand(app, run.ScaffoldTemplates, run.ScaffoldTemplatesBin), ct);
        var plan = await commands.SendAsync(new PlanScaffoldCommand(app, templates.Value, input), ct);
        await commands.SendAsync(new WriteScaffoldCommand(block, plan.Value), ct);
        return [.. log.Findings];
    }
}
