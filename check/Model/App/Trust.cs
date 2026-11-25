namespace Umpire.CheckModel;

using System.Collections.Concurrent;

public sealed class Trust {
    readonly AppModel app;
    readonly HelperPlaces helpers;
    readonly Freshness freshness;
    readonly HashSet<string> missing;
    readonly bool hasUnknownState;
    readonly bool hasMissingProject;
    readonly Lazy<bool> isBlockCurrent;
    readonly Lazy<bool> isAllSure;
    readonly ConcurrentDictionary<AppType, bool> sure = new();

    internal Trust(AppModel app, SnapshotSet snapshots, HelperPlaces helpers) {
        this.app = app;
        this.helpers = helpers;
        freshness = new Freshness(snapshots.Saved);
        OldLibraries = [.. snapshots.Libraries.Where(l => l.Snapshot.Source?.IsCurrent(app.Block) != true)];
        missing = new HashSet<string>(
            app.Problems.Select(p => p.Project).Concat(OldLibraries.Select(l => l.Snapshot.Project)), StringComparer.OrdinalIgnoreCase);
        hasUnknownState = app.Problems.Any(p => p.IsState);
        hasMissingProject = hasUnknownState || app.Problems.Any(p => !p.IsLibrary);
        isBlockCurrent = new(() => app.Settings.DefinitionSources.Count > 0 && OldInputs.Count == 0);
        isAllSure = new(() => app.Problems.Count == 0 && OldLibraries.Count == 0 && freshness.AreAllCurrent);
    }

    public IReadOnlyList<LoadedSnapshot> OldLibraries { get; }
    public IReadOnlyList<FileMark> OldInputs =>
        [.. app.Settings.DefinitionSources.Concat(app.Settings.Inputs).Where(mark => !mark.IsCurrent(app.Block))];
    public bool IsBlockCurrent => isBlockCurrent.Value;
    public bool IsAllSure => isAllSure.Value;

    public bool IsSure(AppType type) => sure.GetOrAdd(type, t => HasCurrentBases(t) && HasCurrentNeighbors(t));

    public bool IsSure(TypeName name) => app.Find(name) is { } type
        ? !missing.Contains(name.Assembly) && IsSure(type)
        : IsKnown(name);

    public bool AreSure(IEnumerable<TypeName> names) => names.Distinct().All(IsSure);

    bool HasCurrentBases(AppType type) => IsCurrent(type) && type.Content.Bases.All(b => app.Find(b) is { } found ? IsCurrent(found) : IsKnown(b));

    bool HasCurrentNeighbors(AppType type) => helpers.NeighborsOf(type) is not { } neighbors
        || !hasMissingProject && neighbors.All(HasCurrentBases);

    bool IsCurrent(AppType type) => !missing.Contains(type.Name.Assembly) && freshness.IsCurrent(type);

    bool IsKnown(TypeName name) => !hasUnknownState && !missing.Contains(name.Assembly);
}
