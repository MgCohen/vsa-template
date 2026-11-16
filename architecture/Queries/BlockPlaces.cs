namespace Umpire.Architecture;

static class BlockPlaces {
    public static IReadOnlyList<BlockPlace> Covered(Architecture definition, string blockFolder, IReadOnlyList<string> innerBlocks) {
        var parts = definition.Parts().Where(p => !p.IsExternal && !p.IsBlockPlace && (p.HasFolder || p.IsGroup)).ToList();
        return [.. Relative(blockFolder, innerBlocks)
            .SelectMany(inner => parts
                .Where(part => Covers(part.Path.Split('/'), inner.Split('/')))
                .Select(part => new BlockPlace(part, Full(blockFolder, inner), inner)))];
    }

    public static IReadOnlyList<BlockPlace> Empty(Architecture definition, string blockFolder, IReadOnlyList<string> innerBlocks) {
        var inner = Relative(blockFolder, innerBlocks).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. definition.Parts().Where(p => p.IsBlockPlace)
            .SelectMany(place => Folders(blockFolder, place.Path.Split('/'))
                .Where(folder => !inner.Contains(folder))
                .Select(folder => new BlockPlace(place, Full(blockFolder, folder), folder)))];
    }

    static bool Covers(string[] pattern, string[] inner) =>
        pattern.Length >= inner.Length && inner.Select((folder, i) => Fits(pattern[i], folder)).All(fits => fits);

    static bool Fits(string pattern, string folder) =>
        pattern.StartsWith("{", StringComparison.Ordinal) || string.Equals(pattern, folder, StringComparison.OrdinalIgnoreCase);

    static IEnumerable<string> Folders(string blockFolder, string[] pattern) {
        IEnumerable<string> found = [""];
        foreach (var segment in pattern) {
            found = segment.StartsWith("{", StringComparison.Ordinal)
                ? found.SelectMany(folder => Subfolders(blockFolder, folder))
                : found.Select(folder => Join(folder, segment));
        }
        return found;
    }

    static IEnumerable<string> Subfolders(string blockFolder, string folder) {
        var full = Full(blockFolder, folder);
        if (!Directory.Exists(full)) {
            return [];
        }
        return Directory.GetDirectories(full)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => !BuildOutput.IsFolder(name) && !name.StartsWith(".", StringComparison.Ordinal) && !name.EndsWith("~", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => Join(folder, name));
    }

    static IEnumerable<string> Relative(string blockFolder, IReadOnlyList<string> innerBlocks) =>
        innerBlocks.Select(folder => Path.GetRelativePath(blockFolder, folder).Replace('\\', '/').TrimEnd('/'))
            .Where(folder => folder != "." && !folder.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(folder));

    static string Join(string folder, string segment) => folder.Length == 0 ? segment : $"{folder}/{segment}";

    static string Full(string blockFolder, string folder) => Path.GetFullPath(Path.Combine(blockFolder, folder));
}
