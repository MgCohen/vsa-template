namespace Umpire.CheckModel;

using BlockFolders = Umpire.Architecture.BlockFolders;

public static class BlockPlaceCheck {
    public static IReadOnlyList<string> Patterns(AppModel app) =>
        [.. app.Parts.Where(p => !p.IsExternal && p.IsBlockPlace).Select(p => p.Path).Concat(Inner(app)).Distinct(StringComparer.Ordinal)];

    public static void Run(AppModel app, BlockFolders folders, Report report) {
        var inner = Inner(app);
        var parts = app.Parts.Where(p => !p.IsExternal && (p.HasFolder || p.IsGroup)).ToList();
        Covered(app, parts, inner, folders, report);
        Empty(app, parts, inner, folders, report);
    }

    static IReadOnlyList<string> Inner(AppModel app) => [.. app.InnerBlocks.Where(folder => BlockPath.IsUnder(app.Block, folder))
        .Select(folder => BlockPath.Of(app.Block, folder))];

    static void Covered(AppModel app, IReadOnlyList<Part> parts, IReadOnlyList<string> inner, BlockFolders folders, Report report) {
        foreach (var folder in inner.Where(f => !inner.Any(o => f.StartsWith(o + "/", StringComparison.OrdinalIgnoreCase)))) {
            var segments = folder.Split('/');
            if (CoveringPart(parts, segments) is { } part) {
                Rules.Add(app, report, RuleCodes.CoveredBlock,
                    $"{part.Name} ({part.Path}) covers the folder {folder}, "
                    + $"but a check entry makes {folder} another block. Mark the folder with AddBlock<T>() "
                    + "or AddBlockGroup<T>() in Define, or remove the check entry of the inner block.",
                    BlockPath.In(app.Block, folder), 1, 1,
                    app.Trust.IsAllSure && Exists(folders, folder, folder));
            }
        }
    }

    static void Empty(AppModel app, IReadOnlyList<Part> parts, IReadOnlyList<string> inner, BlockFolders folders, Report report) {
        foreach (var place in parts.Where(p => p.IsBlockPlace)) {
            var hasDllForm = !place.IsGroup && place.IsBlock && place.Children.Count > 0;
            var found = folders.Folders.Where(f => f.Pattern == place.Path && !inner.Contains(f.Folder, StringComparer.OrdinalIgnoreCase));
            foreach (var folder in found.Select(f => f.Folder)) {
                if (hasDllForm && !Exists(folders, place.Path, folder)) {
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

    static bool Exists(BlockFolders folders, string pattern, string folder) =>
        folders.Folders.Any(f => f.Pattern == pattern && string.Equals(f.Folder, folder, StringComparison.OrdinalIgnoreCase) && f.Exists);
}
