namespace Jobs.Slices.CheckJob;

using Blocks.Domain.Shared;
using Blocks.Slices.FindBlocks;
using Build.Slices.BuildApp;
using Changes.Slices.FindChanges;
using Changes.Slices.WriteBlockStamp;
using Changes.Slices.WriteStamp;
using LoadApp.Domain.Shared;
using LoadApp.Slices.LoadApp;
using Operations.Infrastructure;
using Rules.Slices.CheckRules;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Requests;
using Versions.Slices.CheckVersions;

sealed class CheckJobHandler(ICommandSender commands, OperationLog log, RunOptions options)
    : RequestHandler<CheckJobRequest, CheckJobReport> {
    public override async Task<Result<CheckJobReport>> HandleAsync(IEnvelope<CheckJobRequest> envelope) {
        var given = envelope.Message;
        var job = given with { Mode = CheckMode.Of(given.Mode, given.Folder) };
        options.FailFast = job.FailFast;
        options.Apply(
            CheckSettings.Of(job.Folder, job.References, job.SourceRoot, job.IgnoredFolders, job.ProjectChecks));
        var build = await commands.SendAsync(new BuildAppCommand(job.Project, job.Bin), envelope.Cancellation);
        if (build.IsSuccess && !build.Value.Failed) {
            await Check(job, envelope.Cancellation);
        } else {
            log.Skip<FindBlocksCommand, FindBlocksResult>();
        }
        return new CheckJobReport(job.Mode!.Value, [.. log.Findings]);
    }

    async Task Check(CheckJobRequest job, CancellationToken ct) {
        var changes = await commands.SendAsync(new FindChangesCommand(job.Folder, job.Bin, Inputs(job)), ct);
        if (changes.IsSuccess && changes.Value.Same) {
            return;
        }
        var before = log.Findings.Count;
        var used = await Block(job, new FindBlocksCommand(job.Project, job.Bin, job.Block), ct);
        foreach (var folder in used) {
            await Block(job, new FindBlocksCommand(folder, job.Bin, "", IsUsed: true), ct);
        }
        if (changes.IsSuccess && log.OnlyInfoSince(before)) {
            await commands.SendAsync(new WriteStampCommand(job.Bin, changes.Value.Hash), ct);
        }
    }

    async Task<IReadOnlyList<string>> Block(CheckJobRequest job, FindBlocksCommand find, CancellationToken ct) {
        var blocks = await commands.SendAsync(find, ct);
        if (!blocks.IsSuccess) {
            return [];
        }
        var tree = blocks.Value.Blocks;
        var findStamp = new FindBlockStampCommand(tree.Current, blocks.Value.Bin, Inputs(job));
        var stamp = await commands.SendAsync(findStamp, ct);
        if (stamp.IsSuccess) {
            log.Add(stamp.Value.Findings);
        }
        if (stamp.IsSuccess && stamp.Value.Same) {
            return tree.Used;
        }
        var before = log.Findings.Count;
        await Check(job, find, blocks.Value, ct);
        if (stamp.IsSuccess && log.OnlyInfoSince(before)) {
            await commands.SendAsync(new WriteBlockStampCommand(stamp.Value.File, stamp.Value.Hash), ct);
        }
        return tree.Used;
    }

    async Task Check(CheckJobRequest job, FindBlocksCommand find, FindBlocksResult found, CancellationToken ct) {
        var tree = found.Blocks;
        var versions = await commands.SendAsync(new CheckVersionsCommand(tree.Current.Folder, job.Bin), ct);
        var app = versions.IsSuccess
            ? await commands.SendAsync(new LoadAppCommand(tree, found.Bin, job.Skip, find.IsUsed), ct)
            : log.Skip<LoadAppCommand, AppInput>();
        if (app.IsSuccess) {
            await commands.SendAsync(new CheckRulesCommand(app.Value), ct);
        } else {
            log.Skip<CheckRulesCommand, CheckRulesResult>();
        }
    }

    string Inputs(CheckJobRequest job) => options.Inputs(job.Mode, job.Skip, job.Block);
}
