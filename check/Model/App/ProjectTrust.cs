namespace Umpire.CheckModel;

sealed class ProjectTrust {
    readonly Dictionary<string, Lazy<bool>> sure = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> missing;
    readonly bool hasUnknownState;

    public ProjectTrust(SnapshotSet snapshots, string block) {
        missing = new HashSet<string>(snapshots.Problems.Select(p => p.Project), StringComparer.OrdinalIgnoreCase);
        hasUnknownState = snapshots.Problems.Any(p => p.IsState);
        foreach (var project in snapshots.Saved) {
            sure[project.Snapshot.Project] = new(() =>
                !Freshness.OldFiles(project).Any() && project.Snapshot.References.All(IsSure));
        }
        foreach (var library in snapshots.Libraries) {
            sure[library.Snapshot.Project] = new(() => library.Snapshot.Source?.IsCurrent(block) == true);
        }
        Current = snapshots.Current?.Project;
        Projects = [.. snapshots.Saved.Select(s => s.Snapshot.Project)];
    }

    public string? Current { get; }
    public IReadOnlyList<string> Projects { get; }
    public bool HasMissing => missing.Count > 0;
    public bool AreAllSure => !HasMissing && sure.Values.All(s => s.Value);

    public bool IsSure(string assembly) => !missing.Contains(assembly)
        && (sure.TryGetValue(assembly, out var known) ? known.Value : !hasUnknownState || assembly == Current);

    public bool Owns(string assembly) => Projects.Contains(assembly, StringComparer.OrdinalIgnoreCase);
}
