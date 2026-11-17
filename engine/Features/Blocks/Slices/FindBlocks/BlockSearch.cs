namespace Blocks.Slices.FindBlocks;

using Blocks.Domain.Shared;
using BuiltApp.Domain.Shared;
using Steps.Domain.Shared;
using Umpire.Messages.Contracts;

static class BlockSearch {
    public const string Example = "Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa";

    public static bool IsDeclared(string declared) => !string.IsNullOrWhiteSpace(declared);

    public static string? Folder(string project, string declared, IReadOnlyList<BlockEntry> entries) =>
        IsDeclared(declared) ? Full(declared) : Nearest(Full(project), entries);

    public static Result<FindBlocksResult> Find(string project, string? folder, IReadOnlyList<BlockEntry> entries, BinIndex bin) {
        if (folder is null) {
            return new Error(BlocksCodes.NoBlock, NoBlock(project));
        }
        var scoped = entries.Where(e => IsAtOrUnder(e.Folder, folder)).Distinct().ToList();
        var own = scoped.Where(e => Same(e.Folder, folder)).ToList();
        if (Problems(own) is [var stop, ..]) {
            return new Error(stop.Code, stop.Message);
        }
        var inner = scoped.Except(own).ToList();
        var current = own.FirstOrDefault() ?? new BlockEntry(folder, EntryForm.App, null, folder);
        return new FindBlocksResult(Problems(inner), new BlockTree(current with { Folder = folder }, inner), bin);
    }

    public static string NoBlock(string project) =>
        $"No check entry is in {Full(project)} or in a folder above it. "
        + "Add an app class, a Block<TArchitecture> type, the AssemblyMetadata \"Umpire.Architecture\" or an umpire.json file, "
        + "or give the block folder with --block (MSBuild property UmpireBlock).";

    static IReadOnlyList<Finding> Problems(IReadOnlyList<BlockEntry> entries) => [
        .. entries.GroupBy(e => e.Folder, StringComparer.OrdinalIgnoreCase).Select(TwoEntries).OfType<Finding>(),
        .. entries.Where(e => e.Problem is not null).Select(BadArchitecture)];

    static Finding? TwoEntries(IGrouping<string, BlockEntry> block) {
        var entries = block.GroupBy(e => e.Key).Select(g => g.First()).ToList();
        return entries.Count > 1
            ? Error(BlocksCodes.TwoEntries, entries[0].Source,
                $"The block {block.Key} has more than one check entry: {string.Join(", ", entries)}. "
                + "A block has exactly one check entry. Keep one, or move one to its own folder.")
            : null;
    }

    static Finding BadArchitecture(BlockEntry entry) =>
        Error(BlocksCodes.BadArchitecture, entry.Source,
            $"{Capital(entry.ToString())} {entry.Problem}. "
            + $"Name the architecture with its full type name and its assembly, for example \"{Example}\".");

    static Finding Error(string code, string file, string message) => new(code, FindingSeverity.Error, message, file);

    static string? Nearest(string project, IReadOnlyList<BlockEntry> entries) => entries
        .Select(e => e.Folder)
        .Where(folder => IsAtOrUnder(project, folder))
        .OrderByDescending(folder => folder.Length)
        .FirstOrDefault();

    static bool IsAtOrUnder(string path, string folder) =>
        Same(path, folder) || path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    static string Full(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
