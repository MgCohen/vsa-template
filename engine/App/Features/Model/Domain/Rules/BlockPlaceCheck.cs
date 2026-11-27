namespace Umpire.CheckModel;

public static class BlockPlaceCheck {
    public static void Run(AppModel app, Report report) {
        var inner = app.InnerBlocks.Where(folder => BlockPath.IsUnder(app.Block, folder))
            .Select(folder => BlockPath.Of(app.Block, folder)).ToList();
        var parts = app.Parts.Where(p => !p.IsExternal && (p.HasFolder || p.IsGroup)).ToList();
        Covered(app, parts, inner, report);
        Empty(app, parts, inner, report);
    }

    static void Covered(AppModel app, IReadOnlyList<Part> parts, IReadOnlyList<string> inner, Report report) {
        foreach (var folder in inner.Where(f => !inner.Any(o => f.StartsWith(o + "/", StringComparison.OrdinalIgnoreCase)))) {
            var segments = folder.Split('/');
            if (CoveringPart(parts, segments) is { } part) {
                Rules.Add(app, report, RuleCodes.CoveredBlock,
                    $"{part.Name} ({part.Path}) covers the folder {folder}, "
                    + $"but a check entry makes {folder} another block. Mark the folder with AddBlock<T>() "
                    + "or AddBlockGroup<T>() in Define, or remove the check entry of the inner block.",
                    BlockPath.In(app.Block, folder), 1, 1,
                    app.Trust.IsAllSure && Directory.Exists(BlockPath.In(app.Block, folder)));
            }
        }
    }

    static void Empty(AppModel app, IReadOnlyList<Part> parts, IReadOnlyList<string> inner, Report report) {
        foreach (var place in parts.Where(p => p.IsBlockPlace)) {
            var hasDllForm = !place.IsGroup && place.IsBlock && place.Children.Count > 0;
            foreach (var folder in Folders(app.Block, place.Path.Split('/')).Where(f => !inner.Contains(f, StringComparer.OrdinalIgnoreCase))) {
                if (hasDllForm && !Directory.Exists(BlockPath.In(app.Block, folder))) {
                    continue;
                }
                Rules.Add(app, report, RuleCodes.EmptyBlockPlace,
                    $"{place.Name} ({place.Path}) marks the folder {folder} as the place of a block, "
                    + "but no check entry is in it. Add a check entry to the folder, or remove the place from Define.",
                    BlockPath.In(app.Block, folder), 1, 1, app.Trust.IsAllSure);
            }
        }
    }

    static Part? CoveringPart(IReadOnlyList<Part> parts, string[] inner) {
        if (parts.Any(p => p.IsBlockPlace && p.Path.Split('/').Length == inner.Length && Overlaps(p, inner))) {
            return null;
        }
        var overlapping = parts.Where(p => !p.IsBlockPlace && Overlaps(p, inner)).ToList();
        return overlapping.Where(p => p.Path.Split('/').Length <= inner.Length)
                .OrderByDescending(p => p.Path.Split('/').Length).FirstOrDefault()
            ?? overlapping.OrderBy(p => p.Path.Split('/').Length).FirstOrDefault();
    }

    static bool Overlaps(Part part, string[] inner) =>
        part.Path.Split('/').Zip(inner, FolderMatch.Fits).All(fits => fits);

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
        var full = BlockPath.In(block, folder);
        return Directory.Exists(full)
            ? Directory.GetDirectories(full).Select(Path.GetFileName).Where(name => !BlockPath.IsIgnored(name))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).Select(name => Join(folder, name))
            : [];
    }

    static string Join(string folder, string segment) => folder.Length == 0 ? segment : $"{folder}/{segment}";
}
