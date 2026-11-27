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

    public static IReadOnlyList<BlockPlace> Empty(ResolvedArchitecture resolved, string blockFolder, IReadOnlyList<string> innerBlocks) {
        var inner = new HashSet<string>(Relative(blockFolder, innerBlocks), StringComparer.OrdinalIgnoreCase);
        return [.. resolved.Parts.Where(p => p.IsBlockPlace)
            .SelectMany(place => Folders(blockFolder, place.Path.Split('/'))
                .Where(folder => !inner.Contains(folder) && (Directory.Exists(Full(blockFolder, folder)) || !HasDllForm(place)))
                .Select(folder => new BlockPlace(place, Full(blockFolder, folder), folder)))];
    }

    static bool HasDllForm(Part place) => !place.IsGroup && place.Concept is Blocks.Block && place.Children.Count > 0;

    static bool Overlaps(string[] pattern, string[] inner) =>
        Enumerable.Range(0, Math.Min(pattern.Length, inner.Length)).All(i => Fits(pattern[i], inner[i]));

    static bool Same(string[] pattern, string[] inner) => pattern.Length == inner.Length && Overlaps(pattern, inner);

    static bool Contains(string outer, string inner) => inner.StartsWith(outer + "/", StringComparison.OrdinalIgnoreCase);

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
            .Where(name => !BuildOutput.IsIgnored(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => Join(folder, name));
    }

    static IEnumerable<string> Relative(string blockFolder, IReadOnlyList<string> innerBlocks) =>
        innerBlocks.Select(folder => RelativePath.Of(blockFolder, folder).Replace('\\', '/').TrimEnd('/'))
            .Where(folder => folder != "." && !folder.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(folder));

    static string Join(string folder, string segment) => folder.Length == 0 ? segment : $"{folder}/{segment}";

    static string Full(string blockFolder, string folder) => Path.GetFullPath(Path.Combine(blockFolder, folder));
}
