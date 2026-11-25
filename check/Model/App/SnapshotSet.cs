namespace Umpire.CheckModel;

sealed class SnapshotSet {
    readonly List<LoadedSnapshot> own = [];
    readonly List<LoadedSnapshot> others = [];
    readonly List<LoadedSnapshot> libraries = [];
    readonly List<SnapshotProblem> problems = [];
    readonly HashSet<string> projects = new(StringComparer.OrdinalIgnoreCase);
    readonly Snapshot? current;

    public SnapshotSet(StateFolder state, BlockSettings settings, Snapshot? current) {
        this.current = current;
        var root = state.BlockOf(settings);
        if (current is not null) {
            projects.Add(current.Project);
            own.Add(new LoadedSnapshot(root, current));
        }
        foreach (var project in settings.Projects.Where(projects.Add)) {
            Load(project, state.SnapshotFile(project), root, own);
        }
        foreach (var other in settings.OtherStates.Select(folder => StateFolder.At(BlockPath.In(state.Folder, folder)))) {
            AddOther(other);
        }
        foreach (var library in settings.Libraries.Where(projects.Add)) {
            Load(library, state.LibraryFile(library), root, libraries);
        }
    }

    public IReadOnlyList<LoadedSnapshot> Own => own;
    public IReadOnlyList<LoadedSnapshot> Libraries => libraries;
    public IReadOnlyList<LoadedSnapshot> All => [.. own, .. others, .. libraries];
    public IReadOnlyList<LoadedSnapshot> Saved => [.. own.Concat(others).Where(s => !ReferenceEquals(s.Snapshot, current))];
    public IReadOnlyList<SnapshotProblem> Problems => problems;

    void AddOther(StateFolder other) {
        try {
            var settings = BlockSettings.Read(StateFile.Read(other.BlockFile));
            foreach (var project in settings.Projects.Where(projects.Add)) {
                Load(project, other.SnapshotFile(project), other.BlockOf(settings), others);
            }
        } catch (Exception e) when (e is FormatException or IOException or UnauthorizedAccessException) {
            problems.Add(new SnapshotProblem(other.Folder, other.BlockFile, e.Message, IsState: true));
        }
    }

    void Load(string project, string file, string root, List<LoadedSnapshot> list) {
        try {
            list.Add(new LoadedSnapshot(root, SnapshotCache.Read(file)));
        } catch (Exception e) when (e is FormatException or IOException or UnauthorizedAccessException) {
            problems.Add(new SnapshotProblem(project, file, e.Message, IsLibrary: ReferenceEquals(list, libraries)));
        }
    }
}
