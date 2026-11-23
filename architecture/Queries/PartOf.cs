namespace Umpire.Architecture;

sealed class PartOf {
    readonly ResolvedArchitecture resolved;
    readonly IReadOnlyList<Part> folderParts;
    readonly IReadOnlyList<Part> blockPlaces;
    readonly IReadOnlyList<Part> dllPlaces;
    readonly IReadOnlyList<FolderAlias> aliases;
    readonly string blockFolder;
    readonly IReadOnlyList<string> innerBlocks;

    PartOf(
        ResolvedArchitecture resolved, string blockFolder, IReadOnlyList<string> innerBlocks,
        IReadOnlyList<FolderAlias> folderAliases) {
        this.resolved = resolved;
        folderParts = [.. resolved.Parts.Where(p => !p.IsExternal && (p.HasFolder || p.IsGroup))];
        blockPlaces = [.. folderParts.Where(p => p.IsBlockPlace)];
        dllPlaces = [.. folderParts.Where(p => (p.IsBlockPlace || p.IsKernelRoot) && !p.IsGroup && p.Concept is Blocks.Block && p.Children.Count > 0)];
        aliases = [.. resolved.Config.FolderAliases, .. folderAliases];
        this.blockFolder = blockFolder;
        this.innerBlocks = [.. innerBlocks.Select(folder => Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar)];
    }

    public static Bindings Bind(
        ResolvedArchitecture resolved, IReadOnlyList<TypeFacts> types, string blockFolder, IReadOnlyList<string> innerBlocks,
        IReadOnlyList<FolderAlias> folderAliases) {
        var partOf = new PartOf(resolved, blockFolder, innerBlocks, folderAliases);
        var bound = new List<Binding>();
        var unmapped = new List<UnmappedType>();
        foreach (var type in types) {
            partOf.Place(type, bound, unmapped);
        }
        return new Bindings(FollowUsers(bound), unmapped);
    }

    void Place(TypeFacts type, List<Binding> bound, List<UnmappedType> unmapped) {
        if (LoadedTypes.IsCompilerGenerated(type.Name)) {
            return;
        }
        var external = resolved.ExternalPart(type.Assembly, IsInBlock(type) ? "" : type.FullName);
        if (type.Source is not { } source || (external is not null && IsInBlock(type))) {
            if (external is not null) {
                bound.Add(ExternalBinding(type, external));
            }
            return;
        }
        var file = Path.GetFullPath(source.File);
        var relative = Path.GetRelativePath(blockFolder, file).Replace('\\', '/');
        if (Outside(relative)) {
            if (external is not null) {
                bound.Add(InPlacedBlock(file, type, source with { File = file }) ?? ExternalBinding(type, external));
            }
            return;
        }
        var paths = PathsInBlock(relative);
        if (paths.Count == 0 || BuildOutput.Contains(relative)) {
            return;
        }
        var located = source with { File = file };
        if (InInnerBlock(file)) {
            if (BlockPlace(paths) is { } place) {
                bound.Add(BindTo(place, type, located));
            }
            return;
        }
        if (TopFile(paths, type) is { } top) {
            bound.Add(new Binding(type, top, new Dictionary<string, string>(), located));
            return;
        }
        var match = Folder(paths);
        if (match is null || match.IsUndeclaredGroupFolder) {
            unmapped.Add(new UnmappedType(type, located));
        } else {
            bound.Add(BindTo(match, type, located));
        }
    }

    static Binding ExternalBinding(TypeFacts type, Part external) => new(type, external, new Dictionary<string, string>(), null);

    static bool Outside(string relative) => relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative);

    Binding? InPlacedBlock(string file, TypeFacts type, Source located) {
        var segments = file.Replace('\\', '/').Split('/');
        var found = dllPlaces
            .SelectMany(place => RootsOf(place, segments).Select(rest => new PlacedRoot(place, rest)))
            .ToList();
        var places = found.Select(f => f.Place).Distinct().ToList();
        if (places.Count > 1) {
            throw new InvalidOperationException(
                $"{type.FullName} ({file}) is in the folders of {string.Join(" and ", places.Select(p => p.Name))}. "
                + "The engine looked in the source paths of a DLL and found two block roots. "
                + "Give one block record a longer Folder, for example \"Vsa/Kernel\".");
        }
        if (found.Count == 0) {
            return null;
        }
        var root = found[0];
        var relative = $"{root.Place.Path}/{root.Rest}";
        var match = Folder([relative], root.Place);
        return match is null ? null : BindTo(match, type, located) with { FromDll = true };
    }

    static IEnumerable<string> RootsOf(Part place, string[] segments) {
        var root = place.Concept!.Folder.Split('/');
        var bodyFolders = place.Children
            .Where(c => c.HasFolder && c.Path.Length > place.Path.Length)
            .Select(c => c.Path[(place.Path.Length + 1)..].Split('/')[0])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var start = 0; start + root.Length < segments.Length - 1; start++) {
            var next = segments[start + root.Length];
            if (root.Select((s, i) => string.Equals(s, segments[start + i], StringComparison.OrdinalIgnoreCase)).All(x => x)
                && bodyFolders.Contains(next)) {
                yield return string.Join("/", segments[(start + root.Length)..]);
            }
        }
    }

    sealed record PlacedRoot(Part Place, string Rest);

    bool IsInBlock(TypeFacts type) =>
        type.Source is { } source && !Path.GetRelativePath(blockFolder, Path.GetFullPath(source.File)).StartsWith("..", StringComparison.Ordinal);

    Part? TopFile(IReadOnlyList<string> paths, TypeFacts type) {
        if (!paths.Any(path => !path.Contains('/'))) {
            return null;
        }
        var top = resolved.Root.Children.FirstOrDefault(p => p.Concept is Blocks.App && !p.HasFolder) ?? resolved.Root;
        var part = Select(top, type);
        return part == resolved.Root ? null : part;
    }

    bool InInnerBlock(string file) => innerBlocks.Any(folder => file.StartsWith(folder, StringComparison.OrdinalIgnoreCase));

    FolderMatch? BlockPlace(IReadOnlyList<string> paths) => paths
        .Select(path => path.Split('/')[..^1])
        .SelectMany(folders => blockPlaces
            .Select(p => new FolderMatch(p, p.Path.Split('/'), folders))
            .Where(m => m.Matches))
        .OrderByDescending(m => m.Pattern.Length)
        .FirstOrDefault();

    List<string> PathsInBlock(string relative) {
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)) {
            return [];
        }
        return aliases.Count == 0 ? [relative] : AliasedPaths(relative);
    }

    List<string> AliasedPaths(string relative) => [.. aliases
        .Where(alias => IsRoot(alias)
            || relative.StartsWith(alias.Folder.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase))
        .Select(alias => alias.As.Length == 0 ? Rest(alias, relative) : $"{alias.As.TrimEnd('/')}/{Rest(alias, relative)}")];

    static bool IsRoot(FolderAlias alias) => alias.Folder.TrimEnd('/').Length == 0;

    static string Rest(FolderAlias alias, string relative) =>
        IsRoot(alias) ? relative : relative[(alias.Folder.TrimEnd('/').Length + 1)..];

    FolderMatch? Folder(IReadOnlyList<string> paths, Part? within = null) => paths
        .Select(path => path.Split('/')[..^1])
        .SelectMany(folders => folderParts
            .Where(p => within is null || p.SelfAndAncestors().Contains(within))
            .Select(p => new FolderMatch(p, p.Path.Split('/'), folders))
            .Where(m => m.Matches))
        .OrderByDescending(m => m.Pattern.Length)
        .ThenBy(m => m.Pattern.Count(segment => segment.StartsWith('{')))
        .FirstOrDefault();

    static Binding BindTo(FolderMatch match, TypeFacts type, Source source) {
        var part = Select(match.Part, type);
        var values = part.Groups.ToDictionary(i => i.Name, i => match.Folders[i.Path.Split('/').Length - 1]);
        return new Binding(type, part, values, source);
    }

    static Part Select(Part folderPart, TypeFacts type) {
        var shared = ParentFolderChildren(folderPart).Where(p => p.Types.Any(type.Is)).ToList();
        if (shared.Count > 1) {
            throw new InvalidOperationException(
                $"{type.FullName} matches {string.Join(" and ", shared.Select(p => p.Name))}, which share one folder. Give each part a different type.");
        }
        return shared.Count == 1 ? shared[0] : folderPart;
    }

    static List<Binding> FollowUsers(List<Binding> bound) {
        var changed = true;
        while (changed) {
            changed = FollowOnce(bound);
        }
        return bound;
    }

    static bool FollowOnce(List<Binding> bound) {
        var changed = false;
        for (var i = 0; i < bound.Count; i++) {
            if (UserPart(bound, bound[i]) is { } target) {
                bound[i] = bound[i] with { Part = target, FollowsUser = true };
                changed = true;
            }
        }
        return changed;
    }

    static Part? UserPart(IReadOnlyList<Binding> bound, Binding helper) {
        var shared = ParentFolderChildren(helper.Part).ToList();
        if (shared.Count == 0) {
            return null;
        }
        var neighbors = bound.Where(b => shared.Contains(b.Part) && b.Path == helper.Path).ToList();
        var users = neighbors.Where(b => b.Type.Mentions.Contains(helper.Type.FullName)).Select(b => b.Part).Distinct();
        return users.OrderBy(p => p.Types.Count == 0).ThenBy(shared.IndexOf).FirstOrDefault() ?? BasePart(neighbors, helper);
    }

    static Part? BasePart(IReadOnlyList<Binding> neighbors, Binding helper) => helper.Type.Bases
        .Skip(1)
        .Select(name => neighbors.FirstOrDefault(b => b.Type.FullName == name))
        .FirstOrDefault(b => b is not null)?.Part;

    static IEnumerable<Part> ParentFolderChildren(Part part) =>
        part.Children.Where(c => !c.HasFolder && !c.IsGroup).SelectMany(c => new[] { c }.Concat(ParentFolderChildren(c)));

    sealed record FolderMatch(Part Part, string[] Pattern, string[] Folders) {
        public bool Matches => Pattern.Length <= Folders.Length && Pattern.Select((s, i) => Fits(s, Folders[i])).All(x => x);

        public bool IsUndeclaredGroupFolder => Part.IsGroup && Folders.Length > Pattern.Length;

        static bool Fits(string pattern, string folder) =>
            pattern.StartsWith('{') || string.Equals(pattern, folder, StringComparison.OrdinalIgnoreCase);
    }
}
