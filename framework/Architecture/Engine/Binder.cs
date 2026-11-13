using ArchUnitNET.Domain;

namespace VSA.Architecture.Engine;

public static class Binder {
    public static List<Binding> Bind(ArchitectureDefinition definition, IEnumerable<IType> types, Dictionary<string, Source> sources,
        string appRoot, List<string> outside, List<UnmappedType> unmapped) {
        var folderParts = definition.Parts().Where(p => !p.IsExternal && (p.HasFolder || p.IsGroup)).ToList();
        var externalParts = definition.Parts().Where(p => p.IsExternal).ToList();
        var rootPath = definition.Root.Path;
        var aliases = definition.FolderAliases(appRoot);
        var bound = new List<Binding>();
        foreach (var type in types) {
            var assembly = type.Assembly.Name.Split(',')[0];
            var external = externalParts
                .Where(p => assembly == p.Assembly || assembly.StartsWith(p.Assembly + ".", StringComparison.Ordinal))
                .OrderByDescending(p => p.Assembly!.Length)
                .FirstOrDefault();
            if (external is not null) {
                if (!type.Name.StartsWith('<')) {
                    bound.Add(new Binding(type, external, new Dictionary<string, string>(), new Source(assembly, 0), External: true));
                }
                continue;
            }
            if (type.Name.StartsWith('<') || !sources.TryGetValue(type.FullName, out var source)) {
                continue;
            }
            var relative = Path.GetRelativePath(appRoot, source.File).Replace('\\', '/');
            if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)) {
                outside.Add(type.FullName);
                continue;
            }
            var candidates = aliases.Count == 0 ? [relative] : AliasedPaths(relative, aliases);
            if (candidates.Count == 0 || !candidates.Any(path => IsUnder(path, rootPath))) {
                outside.Add(type.FullName);
                continue;
            }
            if (relative.Split('/')[..^1].Any(f => f.StartsWith("obj", StringComparison.Ordinal) || f.StartsWith("bin", StringComparison.Ordinal))) {
                continue;
            }

            var match = candidates.Where(path => IsUnder(path, rootPath))
                .Select(path => path.Split('/')[..^1])
                .SelectMany(folders => folderParts
                    .Select(p => new { Part = p, Pattern = p.Path.Split('/'), Folders = folders })
                    .Where(m => m.Pattern.Length <= folders.Length && m.Pattern.Select((s, i) => Matches(s, folders[i])).All(x => x)))
                .OrderByDescending(m => m.Pattern.Length)
                .FirstOrDefault();
            var undeclared = match is not null && match.Part.IsGroup && match.Folders.Length > match.Pattern.Length;
            if (match is null || undeclared) {
                unmapped.Add(new UnmappedType(type, source with { File = relative }));
                continue;
            }

            var part = Select(match.Part, type);
            var values = part.Groups.ToDictionary(i => i.Name, i => match.Folders[i.Path.Split('/').Length - 1]);
            bound.Add(new Binding(type, part, values, source with { File = relative }));
        }
        return FollowUsers(bound);
    }

    public static bool Is(this IType type, Type target) {
        var name = target.FullName!;
        var bases = new[] { type }
            .Concat(type is Class c ? c.InheritedClasses : [])
            .Concat(type.ImplementedInterfaces);
        return bases.Any(b => b.FullName == name);
    }

    static List<string> AliasedPaths(string relative, IReadOnlyList<FolderAlias> aliases) => [.. aliases
        .Where(alias => relative.StartsWith(alias.Folder.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase))
        .Select(alias => alias.As.Length == 0
            ? relative[(alias.Folder.TrimEnd('/').Length + 1)..]
            : $"{alias.As.TrimEnd('/')}/{relative[(alias.Folder.TrimEnd('/').Length + 1)..]}")];

    static bool IsUnder(string relative, string rootPath) =>
        rootPath.Length == 0 || relative.StartsWith(rootPath.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);

    static bool Matches(string pattern, string folder) =>
        pattern.StartsWith('{') || string.Equals(pattern, folder, StringComparison.OrdinalIgnoreCase);

    static List<Binding> FollowUsers(List<Binding> bound) {
        for (var changed = true; changed;) {
            changed = false;
            for (var i = 0; i < bound.Count; i++) {
                var helper = bound[i];
                var shared = ParentFolderChildren(helper.Part).ToList();
                if (shared.Count == 0) {
                    continue;
                }
                var users = bound.Where(b => shared.Contains(b.Part) && b.Path == helper.Path
                    && b.Type.Dependencies.Any(d => d.Target == helper.Type)).Select(b => b.Part).Distinct().ToList();
                var target = users.OrderBy(p => p.Types.Count == 0).ThenBy(p => shared.IndexOf(p)).FirstOrDefault();
                if (target is null) {
                    continue;
                }
                bound[i] = helper with { Part = target, FollowsUser = true };
                changed = true;
            }
        }
        return bound;
    }

    static Part Select(Part folderPart, IType type) {
        var shared = ParentFolderChildren(folderPart).Where(p => p.Types.Any(t => type.Is(t))).ToList();
        if (shared.Count > 1) {
            throw new InvalidOperationException(
                $"{type.FullName} matches {string.Join(" and ", shared.Select(p => p.Name))}, which share one folder. Give each part a different type.");
        }
        return shared.Count == 1 ? shared[0] : folderPart;
    }

    static IEnumerable<Part> ParentFolderChildren(Part part) =>
        part.Children.Where(c => !c.HasFolder && !c.IsGroup).SelectMany(c => new[] { c }.Concat(ParentFolderChildren(c)));
}
