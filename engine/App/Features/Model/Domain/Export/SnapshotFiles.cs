namespace Umpire.CheckModel.Export;

public static class SnapshotFiles {
    const string FileName = "snapshot.json";

    public static IReadOnlyList<string> Complete(StateFolder state, IReadOnlyList<SnapshotPlace> own) {
        var problems = new List<string>();
        var read = new Dictionary<string, Snapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (var place in own) {
            try {
                read[place.File] = SnapshotCache.Read(place.File);
            } catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException) {
                problems.Add($"{place.File}: {e.Message}");
            }
        }
        foreach (var place in own.Where(p => read.ContainsKey(p.File))) {
            WithSurfaces(place, read[place.File], own, read);
            StatePointer.Write(place.File, state);
        }
        return problems;
    }

    public static void RemoveOld(IReadOnlyList<SnapshotPlace> before, IReadOnlyList<SnapshotPlace> now) {
        var current = new HashSet<string>(now.Select(p => p.File), StringComparer.OrdinalIgnoreCase);
        var projects = new HashSet<string>(now.Select(p => p.Project), StringComparer.OrdinalIgnoreCase);
        foreach (var old in before.Where(p => projects.Contains(p.Project) && !current.Contains(p.File))) {
            Remove(old.File);
        }
    }

    static void WithSurfaces(
        SnapshotPlace place, Snapshot snapshot, IReadOnlyList<SnapshotPlace> own, Dictionary<string, Snapshot> read) {
        var references = snapshot.References.Select(reference => reference.Surface.Length > 0
            || SameTarget(own, reference.Name, place.Target) is not { } other
            || !read.TryGetValue(other.File, out var used)
                ? reference
                : reference with { Surface = Surface.Of(used) }).ToList();
        if (references.SequenceEqual(snapshot.References)) {
            return;
        }
        StateFile.Save(place.File, (snapshot with { References = references }).Write());
    }

    static SnapshotPlace? SameTarget(IReadOnlyList<SnapshotPlace> own, string project, string? target) {
        var places = own.Where(p => string.Equals(p.Project, project, StringComparison.OrdinalIgnoreCase)).ToList();
        return places.FirstOrDefault(p => string.Equals(p.Target, target, StringComparison.OrdinalIgnoreCase)) ?? places.FirstOrDefault();
    }

    static void Remove(string file) {
        try {
            var full = BlockPath.Full(file);
            if (string.Equals(Path.GetFileName(full), FileName, StringComparison.OrdinalIgnoreCase) && File.Exists(full)) {
                File.Delete(full);
                File.Delete(StatePointer.FileOf(full));
            }
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            return;
        }
    }
}
