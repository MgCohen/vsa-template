namespace Umpire.CheckModel;

sealed class ProjectTrust {
    readonly AppModel app;
    readonly SnapshotSet snapshots;
    readonly HashSet<string> missing;
    readonly HashSet<string> known;
    readonly HashSet<TypeName> unplaced;
    readonly bool hasUnknownState;
    readonly Lazy<HashSet<string>> old;

    public ProjectTrust(AppModel app, SnapshotSet snapshots) {
        this.app = app;
        this.snapshots = snapshots;
        missing = Names(snapshots.Problems.Select(p => p.Project));
        known = Names(snapshots.All.Select(s => s.Snapshot.Project));
        unplaced = [.. snapshots.Saved.SelectMany(s => s.Snapshot.Unplaced.Select(n => new TypeName(n.Name, s.Snapshot.Project)))];
        hasUnknownState = snapshots.Problems.Any(p => p.IsState);
        old = new(FindOld);
    }

    public bool AreAllSure => missing.Count == 0 && old.Value.Count == 0 && !SourceFiles.HasNew(app);

    public bool IsSure(string assembly) => !missing.Contains(assembly)
        && (known.Contains(assembly) ? assembly == snapshots.Current?.Project || !old.Value.Contains(assembly) : !hasUnknownState);

    public bool IsFound(TypeName name) => app.Find(name) is not null || unplaced.Contains(name)
        || snapshots.Saved.All(s => s.Snapshot.Project != name.Assembly);

    static HashSet<string> Names(IEnumerable<string> names) => new(names, StringComparer.OrdinalIgnoreCase);

    HashSet<string> FindOld() {
        var found = Names(snapshots.Libraries.Where(l => l.Snapshot.Source?.IsCurrent(app.Block) != true).Select(l => l.Snapshot.Project));
        var saved = snapshots.Saved;
        var surfaces = snapshots.Own.Concat(saved).Distinct().ToDictionary(
            s => s.Snapshot.Project, s => new Lazy<string>(() => Surface.Of(s.Snapshot)), StringComparer.OrdinalIgnoreCase);
        var hasNew = saved.Count > 0 && SourceFiles.HasNew(app);
        foreach (var project in saved) {
            if (hasNew || Freshness.OldFiles(project).Any() || project.Snapshot.References.Any(r =>
                missing.Contains(r.Name) || surfaces.TryGetValue(r.Name, out var surface) && surface.Value != r.Surface)) {
                found.Add(project.Snapshot.Project);
            }
        }
        bool Uses(LoadedSnapshot project) => project.Snapshot.References.Any(r => found.Contains(r.Name));
        while (saved.FirstOrDefault(p => !found.Contains(p.Snapshot.Project) && Uses(p)) is { } next) {
            found.Add(next.Snapshot.Project);
        }
        return found;
    }
}
