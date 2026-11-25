namespace Umpire.CheckModel;

sealed class Places {
    static readonly IReadOnlyDictionary<string, string> NoValues = new Dictionary<string, string>();

    readonly IReadOnlyList<Part> parts;
    readonly IReadOnlyList<Part> folderParts;
    readonly IReadOnlyList<Part> blockPlaces;
    readonly IReadOnlyList<Part> dllPlaces;
    readonly IReadOnlyList<Part> externals;
    readonly IReadOnlyList<string> innerBlocks;
    readonly FolderPaths paths;

    public Places(IReadOnlyList<Part> parts, FolderPaths paths, IReadOnlyList<string> innerBlocks) {
        this.parts = parts;
        this.paths = paths;
        this.innerBlocks = innerBlocks;
        folderParts = [.. parts.Where(p => !p.IsExternal && (p.HasFolder || p.IsGroup))];
        blockPlaces = [.. folderParts.Where(p => p.IsBlockPlace)];
        dllPlaces = [.. folderParts.Where(p =>
            (p.IsBlockPlace || p.IsKernelRoot) && !p.IsGroup && p.IsBlock && p.Children.Count > 0)];
        externals = [.. parts.Where(p => p.IsExternal).OrderByDescending(p => p.External!.Length)];
    }

    public void Place(AppType type) {
        if (type.Name.Name.StartsWith("<", StringComparison.Ordinal) || paths.IsIgnored(type.File)) {
            return;
        }
        var isInBlock = paths.IsInBlock(type.File);
        var external = ExternalOf(type.Name.Assembly, isInBlock ? "" : type.Name.Namespace);
        if (external is not null) {
            type.Placement = isInBlock ? Of(external, type) : InPlacedBlock(type) ?? Of(external, type);
            return;
        }
        var inBlock = paths.Of(type.File);
        if (inBlock.Count == 0) {
            return;
        }
        if (innerBlocks.Any(folder => BlockPath.IsUnder(folder, type.File))) {
            type.Placement = Best(inBlock, blockPlaces) is { } place ? BindTo(place, type) : null;
            return;
        }
        if (TopPart(inBlock, type) is { } top) {
            type.Placement = new Placement(top, NoValues, type.Name.Assembly);
            return;
        }
        var match = Best(inBlock, folderParts);
        type.IsUnmapped = match is null || match.IsUndeclaredGroupFolder;
        type.Placement = type.IsUnmapped ? null : BindTo(match!, type);
    }

    public Placement? Outside(TypeName type) =>
        ExternalOf(type.Assembly, type.Namespace) is { } external ? new Placement(external, NoValues, type.Assembly) : null;

    static Placement Of(Part external, AppType type) => new(external, NoValues, type.Name.Assembly);

    static Placement BindTo(FolderMatch match, AppType type, bool isFromDll = false) {
        var part = Select(match.Part, type);
        return new Placement(part, match.ValuesOf(part), type.Name.Assembly, isFromDll);
    }

    static Part Select(Part folderPart, AppType type) => Shared(folderPart.InThisFolder, type) ?? folderPart;

    static Part? Shared(IReadOnlyList<Part> inFolder, AppType type) {
        var shared = inFolder.Where(p => p.Types.Any(type.Is)).ToList();
        if (shared.Count > 1) {
            type.PlaceProblem = $"{type.Name.FullName} matches {string.Join(" and ", shared.Select(p => p.Name))}, which share one folder. "
                + "Give each part a different type.";
        }
        return shared.FirstOrDefault();
    }

    static FolderMatch? Best(IReadOnlyList<string> paths, IReadOnlyList<Part> parts) => paths
        .Select(FolderMatch.FoldersOf)
        .SelectMany(folders => parts.Select(p => new FolderMatch(p, folders)).Where(m => m.IsMatch))
        .OrderByDescending(m => m.Length)
        .ThenBy(m => m.ValueCount)
        .FirstOrDefault();

    static IEnumerable<string> RootsOf(Part place, string[] segments) {
        var root = place.BodyFolder.Split('/');
        var bodyFolders = new HashSet<string>(
            place.Children.Where(c => c.HasFolder && c.Path.Length > place.Path.Length)
                .Select(c => c.Path.Substring(place.Path.Length + 1).Split('/')[0]),
            StringComparer.OrdinalIgnoreCase);
        for (var start = 0; start + root.Length < segments.Length - 1; start++) {
            var isRoot = !root.Where((s, i) => !string.Equals(s, segments[start + i], StringComparison.OrdinalIgnoreCase)).Any();
            if (isRoot && bodyFolders.Contains(segments[start + root.Length])) {
                yield return string.Join("/", segments.Skip(start + root.Length));
            }
        }
    }

    static bool IsUnder(string name, string external) =>
        name == external || name.StartsWith(external + ".", StringComparison.Ordinal);

    Placement? InPlacedBlock(AppType type) {
        var segments = type.File.Replace('\\', '/').Split('/');
        var found = dllPlaces.SelectMany(place => RootsOf(place, segments).Select(rest => new PlacedRoot(place, rest))).ToList();
        var places = found.Select(f => f.Place).Distinct().ToList();
        if (places.Count > 1) {
            type.PlaceProblem = $"{type.Name.FullName} ({type.File}) is in the folders of {string.Join(" and ", places.Select(p => p.Name))}. "
                + "The check looked in the source paths of a DLL and found two block roots. "
                + "Give one block record a longer Folder, for example \"Vsa/Kernel\".";
        }
        if (found.Count == 0) {
            return null;
        }
        var root = found[0];
        var body = folderParts.Where(p => p.Ancestry.Contains(root.Place)).ToList();
        return Best([$"{root.Place.Path}/{root.Rest}"], body) is { } match ? BindTo(match, type, isFromDll: true) : null;
    }

    Part? ExternalOf(string assembly, string space) =>
        externals.FirstOrDefault(p => IsUnder(assembly, p.External!) || IsUnder(space, p.External!));

    Part? TopPart(IReadOnlyList<string> inBlock, AppType type) {
        if (inBlock.All(path => path.Contains("/"))) {
            return null;
        }
        var top = parts.FirstOrDefault(p => p is { IsApp: true, HasFolder: false, Parent: null });
        var inFolder = top?.InThisFolder
            ?? [.. parts.Where(p => p is { Parent: null, IsInParentFolder: true }).SelectMany(p => new[] { p }.Concat(p.InThisFolder))];
        return Shared(inFolder, type) ?? top;
    }

    sealed record PlacedRoot(Part Place, string Rest);
}
