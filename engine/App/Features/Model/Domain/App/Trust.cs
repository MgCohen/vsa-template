namespace Umpire.CheckModel;

using System.Collections.Concurrent;

public sealed class Trust {
    readonly AppModel app;
    readonly HelperPlaces helpers;
    readonly ProjectTrust projects;
    readonly bool hasMissingProject;
    readonly bool isDefinitionOfBlock;
    readonly Lazy<bool> isBlockCurrent;
    readonly Lazy<bool> isAllSure;
    readonly ConcurrentDictionary<AppType, bool> sure = new();

    internal Trust(AppModel app, SnapshotSet snapshots, HelperPlaces helpers, bool isDefinitionOfBlock) {
        this.app = app;
        this.helpers = helpers;
        this.isDefinitionOfBlock = isDefinitionOfBlock;
        projects = new ProjectTrust(app, snapshots);
        OldLibraries = [.. snapshots.Libraries.Where(l => !Freshness.IsCurrent(l.Snapshot.Source, app.Block, app.Files))];
        hasMissingProject = app.Problems.Any(p => !p.IsLibrary);
        isBlockCurrent = new(() => isDefinitionOfBlock && app.Settings.DefinitionSources.Count > 0 && OldInputs.Count == 0);
        isAllSure = new(() => projects.AreAllSure);
    }

    public IReadOnlyList<LoadedSnapshot> OldLibraries { get; }
    public IReadOnlyList<FileMark> OldInputs =>
        [.. app.Settings.DefinitionSources.Concat(app.Settings.Inputs).Where(mark => !app.Files.IsCurrent(mark, app.Block))];
    public bool IsBlockCurrent => isBlockCurrent.Value;
    public bool IsAllSure => isAllSure.Value;

    public string? BlockProblem => IsBlockCurrent ? null
        : !isDefinitionOfBlock ? "definition.json and block.json are not from one run"
        : app.Settings.DefinitionSources.Count == 0 ? "block.json has no mark of the definition"
        : $"these files changed: {string.Join(", ", OldInputs.Select(mark => mark.Path))}";

    public bool IsSure(string project) => projects.IsSure(project);

    public bool IsSure(AppType type) => sure.GetOrAdd(type, t => HasSureBases(t) && HasSureNeighbors(t));

    public bool IsSure(TypeName name) => projects.IsSure(name.Assembly)
        && projects.IsFound(name)
        && (app.Find(name) is not { } type || IsSure(type));

    public bool AreSure(IEnumerable<TypeName> names) => names.Distinct().All(IsSure);

    bool HasSureBases(AppType type) => projects.IsSure(type.Project)
        && type.Content.Bases.All(b => projects.IsSure(b.Assembly) && projects.IsFound(b));

    bool HasSureNeighbors(AppType type) => helpers.NeighborsOf(type) is not { } neighbors
        || !hasMissingProject && neighbors.All(HasSureBases);
}
