namespace Umpire.Architecture;

sealed class PartOf {
    readonly ResolvedArchitecture resolved;
    readonly IReadOnlyList<Part> folderParts;
    readonly IReadOnlyList<Part> blockPlaces;
    readonly IReadOnlyList<FolderAlias> aliases;
    readonly string blockFolder;
    readonly IReadOnlyList<string> innerBlocks;

    PartOf(ResolvedArchitecture resolved, string blockFolder, IReadOnlyList<string> innerBlocks) {
        var definition = resolved.Architecture;
        this.resolved = resolved;
        folderParts = [.. definition.Parts().Where(p => !p.IsExternal && (p.HasFolder || p.IsGroup))];
        blockPlaces = [.. folderParts.Where(p => p.IsBlockPlace)];
        aliases = definition.FolderAliases(blockFolder);
        this.blockFolder = blockFolder;
        this.innerBlocks = [.. innerBlocks.Select(folder => Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar)];
    }

    public static Bindings Bind(ResolvedArchitecture resolved, IReadOnlyList<TypeFacts> types, string blockFolder, IReadOnlyList<string> innerBlocks) {
        var partOf = new PartOf(resolved, blockFolder, innerBlocks);
        var bound = new List<Binding>();
        var unmapped = new List<UnmappedType>();
        foreach (var type in types) {
            partOf.Place(type, bound, unmapped);
        }
        return new Bindings(FollowUsers(bound), unmapped);
    }

    void Place(TypeFacts type, List<Binding> bound, List<UnmappedType> unmapped) {
        if (type.Name.StartsWith('<')) {
            return;
        }
        if (resolved.ExternalPart(type.Assembly) is { } external) {
            bound.Add(new Binding(type, external, new Dictionary<string, string>(), null));
            return;
        }
        if (type.Source is not { } source) {
            return;
        }
        var file = Path.GetFullPath(source.File);
        var relative = Path.GetRelativePath(blockFolder, file).Replace('\\', '/');
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

    Part? TopFile(IReadOnlyList<string> paths, TypeFacts type) {
        if (!paths.Any(path => !path.Contains('/'))) {
            return null;
        }
        var part = Select(resolved.Architecture.Block, type);
        return part == resolved.Architecture.Block ? null : part;
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
        .Where(alias => relative.StartsWith(alias.Folder.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase))
        .Select(alias => alias.As.Length == 0
            ? relative[(alias.Folder.TrimEnd('/').Length + 1)..]
            : $"{alias.As.TrimEnd('/')}/{relative[(alias.Folder.TrimEnd('/').Length + 1)..]}")];

    FolderMatch? Folder(IReadOnlyList<string> paths) => paths
        .Select(path => path.Split('/')[..^1])
        .SelectMany(folders => folderParts
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
        var users = bound.Where(b => shared.Contains(b.Part) && b.Path == helper.Path && b.Type.Mentions.Contains(helper.Type.FullName))
            .Select(b => b.Part).Distinct();
        return users.OrderBy(p => p.Types.Count == 0).ThenBy(shared.IndexOf).FirstOrDefault();
    }

    static IEnumerable<Part> ParentFolderChildren(Part part) =>
        part.Children.Where(c => !c.HasFolder && !c.IsGroup).SelectMany(c => new[] { c }.Concat(ParentFolderChildren(c)));

    sealed record FolderMatch(Part Part, string[] Pattern, string[] Folders) {
        public bool Matches => Pattern.Length <= Folders.Length && Pattern.Select((s, i) => Fits(s, Folders[i])).All(x => x);

        public bool IsUndeclaredGroupFolder => Part.IsGroup && Folders.Length > Pattern.Length;

        static bool Fits(string pattern, string folder) =>
            pattern.StartsWith('{') || string.Equals(pattern, folder, StringComparison.OrdinalIgnoreCase);
    }
}
