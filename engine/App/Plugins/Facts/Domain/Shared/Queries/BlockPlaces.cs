namespace Umpire.Architecture;

using Umpire.Utility;

static class BlockPlaces {
    public static IReadOnlyList<BlockPlace> Covered(
        ResolvedArchitecture resolved, string blockFolder, IReadOnlyList<string> innerBlocks) {
        var parts = resolved.Parts.Where(p => !p.IsExternal && (p.HasFolder || p.IsGroup)).ToList();
        var inner = Relative(blockFolder, innerBlocks).ToList();
        var outermost = inner.Where(folder => !inner.Any(other => other != folder && Contains(other, folder)));
        var covered = new List<BlockPlace>();
        foreach (var folder in outermost) {
            var segments = folder.Split('/');
            if (parts.Any(p => p.IsBlockPlace && Same(p.Path.Split('/'), segments))) {
                continue;
            }
            var overlapping = parts.Where(p => !p.IsBlockPlace && Overlaps(p.Path.Split('/'), segments)).ToList();
            var part = overlapping.Where(p => p.Path.Split('/').Length <= segments.Length)
                .OrderByDescending(p => p.Path.Split('/').Length)
                .FirstOrDefault()
                ?? overlapping.OrderBy(p => p.Path.Split('/').Length).FirstOrDefault();
            if (part is not null) {
                covered.Add(new BlockPlace(part, Full(blockFolder, folder), folder));
            }
        }
        return covered;
    }

    public static IReadOnlyList<string> Patterns(ResolvedArchitecture resolved) =>
        [.. resolved.Parts.Where(p => p.IsBlockPlace).Select(p => p.Path)];

    public static IReadOnlyList<BlockPlace> Empty(
        ResolvedArchitecture resolved, string blockFolder, IReadOnlyList<string> innerBlocks, BlockFolders folders) {
        var inner = new HashSet<string>(Relative(blockFolder, innerBlocks), StringComparer.OrdinalIgnoreCase);
        return [.. resolved.Parts.Where(p => p.IsBlockPlace)
            .SelectMany(place => folders.Folders.Where(f => f.Pattern == place.Path)
                .Where(f => !inner.Contains(f.Folder) && (f.Exists || !HasDllForm(place)))
                .Select(f => new BlockPlace(place, Full(blockFolder, f.Folder), f.Folder)))];
    }

    static bool HasDllForm(Part place) => !place.IsGroup && place.Concept is Blocks.Block && place.Children.Count > 0;

    static bool Overlaps(string[] pattern, string[] inner) =>
        Enumerable.Range(0, Math.Min(pattern.Length, inner.Length)).All(i => Fits(pattern[i], inner[i]));

    static bool Same(string[] pattern, string[] inner) => pattern.Length == inner.Length && Overlaps(pattern, inner);

    static bool Contains(string outer, string inner) => inner.StartsWith(outer + "/", StringComparison.OrdinalIgnoreCase);

    static bool Fits(string pattern, string folder) =>
        pattern.StartsWith("{", StringComparison.Ordinal) || string.Equals(pattern, folder, StringComparison.OrdinalIgnoreCase);

    static IEnumerable<string> Relative(string blockFolder, IReadOnlyList<string> innerBlocks) =>
        innerBlocks.Select(folder => RelativePath.Of(blockFolder, folder).Replace('\\', '/').TrimEnd('/'))
            .Where(folder => folder != "." && !folder.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(folder));

    static string Full(string blockFolder, string folder) => Path.GetFullPath(Path.Combine(blockFolder, folder));
}
