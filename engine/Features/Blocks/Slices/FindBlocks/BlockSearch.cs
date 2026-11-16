namespace Blocks.Slices.FindBlocks;

using Blocks.Domain.Shared;
using BuiltApp.Domain.Shared;
using Steps.Domain.Shared;
using Umpire.Messages.Contracts;

static class BlockSearch {
    public const string Example = "Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa";

    public static string? Folder(string project, string declared, IReadOnlyList<BlockEntry> entries) =>
        string.IsNullOrWhiteSpace(declared) ? Nearest(project, entries) : Full(declared);

    public static Result<FindBlocksResult> Find(
        string project, string? folder, IReadOnlyList<BlockEntry> entries, Func<BlockEntry, string?> problem) {
        if (folder is null) {
            return new Error(BlocksCodes.NoBlock, NoBlock(project));
        }
        var scoped = entries.Where(e => IsAtOrUnder(e.Folder, folder))
            .DistinctBy(e => e.Source, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var problems = Problems(scoped, problem);
        if (problems.FirstOrDefault(p => Same(p.Folder, folder)) is { } own) {
            return new Error(own.Finding.Code, own.Finding.Message);
        }
        var current = scoped.FirstOrDefault(e => Same(e.Folder, folder)) ?? new BlockEntry(folder, EntryForm.App, null, folder);
        var inner = scoped.Where(e => !Same(e.Folder, folder)).ToList();
        var blocks = new BlockTree(current with { Folder = folder }, inner);
        return new FindBlocksResult([.. problems.Select(p => p.Finding)], blocks);
    }

    public static string NoBlock(string project) =>
        $"No check entry is in {Full(project)} or in a folder above it. "
        + "Add an app class, a Block<TArchitecture> type, the AssemblyMetadata \"Umpire.Architecture\" or an umpire.json file, "
        + "or give the block folder with --block (MSBuild property UmpireBlock).";

    static List<FolderProblem> Problems(IReadOnlyList<BlockEntry> scoped, Func<BlockEntry, string?> problem) {
        var problems = new List<FolderProblem>();
        foreach (var block in scoped.GroupBy(e => Full(e.Folder), StringComparer.OrdinalIgnoreCase)) {
            var entries = block.GroupBy(e => e.Key).Select(g => g.First()).ToList();
            if (entries.Count > 1) {
                problems.Add(new FolderProblem(block.Key, Error(BlocksCodes.TwoEntries, entries[0].Source,
                    $"The block {block.Key} has more than one check entry: {string.Join(", ", entries)}. "
                    + "A block has exactly one check entry. Keep one, or move one to its own folder.")));
            }
        }
        foreach (var entry in scoped) {
            if (problem(entry) is { } text) {
                problems.Add(new FolderProblem(Full(entry.Folder), Error(BlocksCodes.BadArchitecture, entry.Source,
                    $"{Capital(entry.ToString())} {text}. "
                    + $"Name the architecture with its full type name and its assembly, for example \"{Example}\".")));
            }
        }
        return problems;
    }

    static Finding Error(string code, string file, string message) => new(code, FindingSeverity.Error, message, file);

    static string? Nearest(string project, IReadOnlyList<BlockEntry> entries) => entries
        .Select(e => Full(e.Folder))
        .Where(folder => IsAtOrUnder(project, folder))
        .OrderByDescending(folder => folder.Length)
        .FirstOrDefault();

    static bool IsAtOrUnder(string path, string folder) {
        var full = Full(path);
        var root = Full(folder);
        return Same(full, root) || full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    static bool Same(string a, string b) => string.Equals(Full(a), Full(b), StringComparison.OrdinalIgnoreCase);

    static string Full(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    sealed record FolderProblem(string Folder, Finding Finding);
}
