namespace Blocks.Slices.FindBlocks;

using Blocks.Domain.Shared;
using Blocks.Infrastructure;
using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using Steps.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class FindBlocksHandler(RunOptions options) : CommandHandler<FindBlocksCommand, FindBlocksResult> {
    public override Task<Result<FindBlocksResult>> HandleAsync(IEnvelope<FindBlocksCommand> envelope) =>
        Task.FromResult(Find(envelope.Message));

    Result<FindBlocksResult> Find(FindBlocksCommand command) {
        var project = Path.GetFullPath(command.Project);
        if (TargetRuntime.NewerThanEngine(Path.GetFullPath(command.Bin)) is { } newer) {
            return new Error(BlocksCodes.NewerRuntime, NewerRuntime(newer));
        }
        using var reader = new BinReader(Path.GetFullPath(command.Bin), new ReadOptions(options.References, options.SourceRoot, options.IgnoredFolders));
        var bin = reader.Index();
        var above = EntryFiles.Above(BlockSearch.IsDeclared(command.Declared) ? Path.GetFullPath(command.Declared) : project);
        var folder = BlockSearch.Folder(project, command.Declared, [.. bin.Entries, .. above]);
        IReadOnlyList<BlockEntry> files = folder is null ? above : [.. above, .. EntryFiles.Below(folder)];
        IReadOnlyList<BlockEntry> entries = [.. bin.Entries, .. files.Select(reader.Check)];
        var found = BlockSearch.Find(project, folder, entries, bin);
        if (!found.IsSuccess || command.IsUsed) {
            return found;
        }
        var tree = found.Value.Blocks;
        var used = UsedBlocks.Of(tree.Current.Folder, tree.Inner, entries, bin);
        return WithProjects(found.Value with { Blocks = tree with { Used = used } });
    }

    public static string NewerRuntime(NewerDll newer) =>
        $"{Path.GetFileName(newer.Dll)} targets .NET {newer.Runtime.Major}, and the engine runs on .NET {Environment.Version}, "
        + $"so the engine cannot load the app. Install .NET {newer.Runtime.Major}, "
        + "or run the engine with 'dotnet exec --roll-forward LatestMajor'.";

    FindBlocksResult WithProjects(FindBlocksResult found) {
        if (!options.ProjectChecks) {
            return found;
        }
        var folder = found.Blocks.Current.Folder;
        var projects = BlockProjects.In(folder, found.Blocks.InnerFolders);
        var checks = ProjectCheck.Findings(folder, projects, BlockProjects.HasConfigFile(folder));
        return found with { Findings = [.. found.Findings, .. checks] };
    }
}
