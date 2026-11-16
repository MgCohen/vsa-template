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
        using var reader = new EntryReader(Path.GetFullPath(command.Bin));
        var files = EntryFiles.Above(project).ToList();
        var entries = reader.Entries().Concat(files.Select(f => f.Entry)).ToList();
        var folder = BlockSearch.Folder(project, command.Declared, entries);
        if (folder is not null) {
            var below = EntryFiles.Below(folder).Concat(EntryFiles.Above(folder)).ToList();
            files.AddRange(below);
            entries.AddRange(below.Select(f => f.Entry));
        }
        var fileProblems = files.Where(f => f.Problem is not null)
            .GroupBy(f => f.Entry.Source, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Problem!, StringComparer.OrdinalIgnoreCase);
        return BlockSearch.Find(project, folder, entries, entry => Problem(entry, fileProblems, reader));
    }

    static string? Problem(BlockEntry entry, Dictionary<string, string> fileProblems, EntryReader reader) {
        if (fileProblems.TryGetValue(entry.Source, out var file)) {
            return $"does not name an architecture: {file}";
        }
        if (entry.Form is EntryForm.App or EntryForm.Base || entry.Architecture is null) {
            return null;
        }
        return reader.Problem(entry.Architecture) is { } problem
            ? $"names \"{entry.Architecture}\", but {problem}"
            : null;
    }
}
