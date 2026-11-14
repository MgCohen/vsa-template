using ArchUnitNET.Domain;

namespace VSA.Architecture.Engine;

public sealed class Binder {
    readonly IReadOnlyList<Part> folderParts;
    readonly IReadOnlyList<Part> externalParts;
    readonly string rootPath;
    readonly IReadOnlyList<FolderAlias> aliases;
    readonly IReadOnlyDictionary<string, Source> sources;
    readonly string appRoot;

    Binder(ArchitectureDefinition definition, IReadOnlyDictionary<string, Source> sources, string appRoot) {
        folderParts = [.. definition.Parts().Where(p => !p.IsExternal && (p.HasFolder || p.IsGroup))];
        externalParts = [.. definition.Parts().Where(p => p.IsExternal)];
        rootPath = definition.Root.Path;
        aliases = definition.FolderAliases(appRoot);
        this.sources = sources;
        this.appRoot = appRoot;
    }

    public static Bindings Bind(ArchitectureDefinition definition, LoadedApp app) {
        var binder = new Binder(definition, app.Sources, app.AppRoot);
        var places = app.Types.Select(binder.Place).ToList();
        return new Bindings(
            FollowUsers([.. places.Select(p => p.Bound).OfType<Binding>()]),
            [.. places.Select(p => p.Unmapped).OfType<UnmappedType>()]);
    }

    public static bool Is(IType type, Type target) {
        var name = target.FullName!;
        var bases = new[] { type }
            .Concat(type is Class c ? c.InheritedClasses : [])
            .Concat(type.ImplementedInterfaces);
        return bases.Any(b => b.FullName == name);
    }

    TypePlace Place(IType type) {
        if (ExternalPart(type) is { } external) {
            return type.Name.StartsWith('<') ? TypePlace.None : TypePlace.Of(ExternalBinding(type, external));
        }
        if (type.Name.StartsWith('<') || !sources.TryGetValue(type.FullName, out var source)) {
            return TypePlace.None;
        }
        var relative = Path.GetRelativePath(appRoot, source.File).Replace('\\', '/');
        var paths = PathsUnderRoot(relative);
        if (paths.Count == 0 || IsBuildOutput(relative)) {
            return TypePlace.None;
        }
        var located = source with { File = relative };
        var match = Match(paths);
        return match is null || match.IsUndeclaredGroupFolder
            ? TypePlace.Of(new UnmappedType(type, located))
            : TypePlace.Of(BindTo(match, type, located));
    }

    Part? ExternalPart(IType type) {
        var assembly = AssemblyName(type);
        return externalParts
            .Where(p => assembly == p.Assembly || assembly.StartsWith(p.Assembly + ".", StringComparison.Ordinal))
            .OrderByDescending(p => p.Assembly!.Length)
            .FirstOrDefault();
    }

    static Binding ExternalBinding(IType type, Part external) =>
        new(type, external, new Dictionary<string, string>(), new Source(AssemblyName(type), 0), External: true);

    List<string> PathsUnderRoot(string relative) {
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)) {
            return [];
        }
        var candidates = aliases.Count == 0 ? [relative] : AliasedPaths(relative);
        return [.. candidates.Where(IsUnderRoot)];
    }

    List<string> AliasedPaths(string relative) => [.. aliases
        .Where(alias => relative.StartsWith(alias.Folder.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase))
        .Select(alias => alias.As.Length == 0
            ? relative[(alias.Folder.TrimEnd('/').Length + 1)..]
            : $"{alias.As.TrimEnd('/')}/{relative[(alias.Folder.TrimEnd('/').Length + 1)..]}")];

    bool IsUnderRoot(string relative) =>
        rootPath.Length == 0 || relative.StartsWith(rootPath.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);

    static bool IsBuildOutput(string relative) =>
        relative.Split('/')[..^1].Any(f => f.StartsWith("obj", StringComparison.Ordinal) || f.StartsWith("bin", StringComparison.Ordinal));

    FolderMatch? Match(IReadOnlyList<string> paths) => paths
        .Select(path => path.Split('/')[..^1])
        .SelectMany(folders => folderParts
            .Select(p => new FolderMatch(p, p.Path.Split('/'), folders))
            .Where(m => m.Matches))
        .OrderByDescending(m => m.Pattern.Length)
        .FirstOrDefault();

    static Binding BindTo(FolderMatch match, IType type, Source source) {
        var part = Select(match.Part, type);
        var values = part.Groups.ToDictionary(i => i.Name, i => match.Folders[i.Path.Split('/').Length - 1]);
        return new Binding(type, part, values, source);
    }

    static Part Select(Part folderPart, IType type) {
        var shared = ParentFolderChildren(folderPart).Where(p => p.Types.Any(t => Is(type, t))).ToList();
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
        var users = bound.Where(b => shared.Contains(b.Part) && b.Path == helper.Path && Uses(b.Type, helper.Type))
            .Select(b => b.Part).Distinct();
        return users.OrderBy(p => p.Types.Count == 0).ThenBy(shared.IndexOf).FirstOrDefault();
    }

    static bool Uses(IType user, IType target) =>
        user.Dependencies.Any(d => d.Target == target || d.TargetGenericArguments.Any(g => g.Type == target));

    static IEnumerable<Part> ParentFolderChildren(Part part) =>
        part.Children.Where(c => !c.HasFolder && !c.IsGroup).SelectMany(c => new[] { c }.Concat(ParentFolderChildren(c)));

    static string AssemblyName(IType type) => type.Assembly.Name.Split(',')[0];

    sealed record TypePlace(Binding? Bound, UnmappedType? Unmapped) {
        public static readonly TypePlace None = new(null, null);

        public static TypePlace Of(Binding bound) => new(bound, null);

        public static TypePlace Of(UnmappedType unmapped) => new(null, unmapped);
    }

    sealed record FolderMatch(Part Part, string[] Pattern, string[] Folders) {
        public bool Matches => Pattern.Length <= Folders.Length && Pattern.Select((s, i) => Fits(s, Folders[i])).All(x => x);

        public bool IsUndeclaredGroupFolder => Part.IsGroup && Folders.Length > Pattern.Length;

        static bool Fits(string pattern, string folder) =>
            pattern.StartsWith('{') || string.Equals(pattern, folder, StringComparison.OrdinalIgnoreCase);
    }
}
