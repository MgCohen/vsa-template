namespace Blocks.Slices.FindBlocks;

using Blocks.Domain.Shared;
using Blocks.Infrastructure;
using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class FindBlocksHandler : CommandHandler<FindBlocksCommand, FindBlocksResult> {
    public override Task<Result<FindBlocksResult>> HandleAsync(Envelope<FindBlocksCommand> envelope) =>
        Task.FromResult(Find(envelope.Message));

    static Result<FindBlocksResult> Find(FindBlocksCommand command) {
        var project = Path.GetFullPath(command.Project);
        using var reader = new BinReader(Path.GetFullPath(command.Bin));
        var bin = reader.Index();
        var above = EntryFiles.Above(BlockSearch.IsDeclared(command.Declared) ? Path.GetFullPath(command.Declared) : project);
        var folder = BlockSearch.Folder(project, command.Declared, [.. bin.Entries, .. above]);
        IReadOnlyList<BlockEntry> files = folder is null ? above : [.. above, .. EntryFiles.Below(folder)];
        return BlockSearch.Find(project, folder, [.. bin.Entries, .. files.Select(reader.Check)], bin);
    }
}
