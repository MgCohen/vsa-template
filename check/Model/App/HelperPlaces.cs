namespace Umpire.CheckModel;

sealed class HelperPlaces {
    readonly Dictionary<string, List<AppType>> byPath = new(StringComparer.Ordinal);
    readonly HashSet<string> followed = new(StringComparer.Ordinal);

    public HelperPlaces(IEnumerable<AppType> types) {
        foreach (var type in types) {
            if (type.Placement is not { } placement || !IsShared(placement.Part)) {
                continue;
            }
            if (!byPath.TryGetValue(placement.Path, out var list)) {
                list = [];
                byPath.Add(placement.Path, list);
            }
            list.Add(type);
        }
    }

    public IReadOnlyList<AppType>? NeighborsOf(AppType type) =>
        type.Placement is { } placement && byPath.TryGetValue(placement.Path, out var neighbors) ? neighbors : null;

    public Placement? Of(AppType type) {
        if (type.Placement is not { } placement) {
            return null;
        }
        lock (byPath) {
            if (byPath.TryGetValue(placement.Path, out var neighbors) && followed.Add(placement.Path)) {
                Follow(neighbors);
            }
        }
        return type.Placement;
    }

    static bool IsShared(Part part) => part.IsInParentFolder || part.InThisFolder.Count > 0;

    static void Follow(IReadOnlyList<AppType> neighbors) {
        var changed = true;
        while (changed) {
            changed = false;
            foreach (var type in neighbors) {
                if (UserPart(neighbors, type) is { } part) {
                    type.Placement = type.Placement!.Follow(part);
                    changed = true;
                }
            }
        }
    }

    static Part? UserPart(IReadOnlyList<AppType> neighbors, AppType helper) {
        var shared = helper.Placement!.Part.InThisFolder.ToList();
        if (shared.Count == 0) {
            return null;
        }
        var users = neighbors.Where(n => shared.Contains(n.Placement!.Part)).ToList();
        return users.Where(n => n.Mentions(helper.Name.FullName)).Select(n => n.Placement!.Part).Distinct()
                .OrderBy(p => p.Types.Count == 0).ThenBy(shared.IndexOf).FirstOrDefault()
            ?? helper.Content.Bases.Skip(1)
                .Select(b => users.FirstOrDefault(n => n.Name.FullName == b.FullName))
                .FirstOrDefault(n => n is not null)?.Placement!.Part;
    }
}
