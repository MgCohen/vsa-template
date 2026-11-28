namespace Facts.Infrastructure;

using Umpire.Architecture;
using Umpire.Utility;

public sealed class BlockFolderSource : IFactSource<BlockFolderQuery, BlockFolders> {
    public BlockFolders Read(BlockFolderQuery query) => new([.. query.Patterns.Distinct(StringComparer.Ordinal)
        .SelectMany(pattern => Folders(query.Block, pattern.Split('/'))
            .Select(folder => new PlaceFolder(pattern, folder, Directory.Exists(Full(query.Block, folder)))))]);

    static IEnumerable<string> Folders(string block, string[] pattern) {
        IEnumerable<string> found = [""];
        foreach (var segment in pattern) {
            found = segment.StartsWith("{", StringComparison.Ordinal)
                ? found.SelectMany(folder => Subfolders(block, folder))
                : found.Select(folder => Join(folder, segment));
        }
        return found;
    }

    static IEnumerable<string> Subfolders(string block, string folder) {
        var full = Full(block, folder);
        if (!Directory.Exists(full)) {
            return [];
        }
        return Directory.GetDirectories(full)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => !BuildOutput.IsIgnored(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => Join(folder, name));
    }

    static string Join(string folder, string segment) => folder.Length == 0 ? segment : $"{folder}/{segment}";

    static string Full(string block, string folder) => Path.GetFullPath(Path.Combine(block, folder));
}
