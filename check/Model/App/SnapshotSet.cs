namespace Umpire.CheckModel;

sealed class SnapshotSet {
    readonly List<LoadedSnapshot> own = [];
    readonly List<LoadedSnapshot> others = [];
    readonly List<SnapshotProblem> problems = [];
    readonly HashSet<string> projects = new(StringComparer.OrdinalIgnoreCase);

    public SnapshotSet(StateFolder state, BlockSettings settings, Snapshot? current) {
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
        foreach (var file in state.LibraryFiles.Where(f => projects.Add(Path.GetFileNameWithoutExtension(f)))) {
            Load(Path.GetFileNameWithoutExtension(file), file, root, others);
        }
    }

    public IReadOnlyList<LoadedSnapshot> Own => own;
    public IReadOnlyList<LoadedSnapshot> All => [.. own, .. others];
    public IReadOnlyList<SnapshotProblem> Problems => problems;

    void AddOther(StateFolder other) {
        if (!File.Exists(other.BlockFile)) {
            problems.Add(new SnapshotProblem(other.Folder, other.BlockFile, "it does not exist"));
            return;
        }
        try {
            var root = other.BlockOf(BlockSettings.Read(File.ReadAllText(other.BlockFile)));
            foreach (var file in other.SnapshotFiles.Where(f => projects.Add(Path.GetFileNameWithoutExtension(f)))) {
                Load(Path.GetFileNameWithoutExtension(file), file, root, others);
            }
        } catch (Exception e) when (e is FormatException or IOException or UnauthorizedAccessException) {
            problems.Add(new SnapshotProblem(other.Folder, other.BlockFile, e.Message));
        }
    }

    void Load(string project, string file, string root, List<LoadedSnapshot> list) {
        if (!File.Exists(file)) {
            problems.Add(new SnapshotProblem(project, file, "it does not exist"));
            return;
        }
        try {
            list.Add(new LoadedSnapshot(root, SnapshotCache.Read(file)));
        } catch (Exception e) when (e is FormatException or IOException or UnauthorizedAccessException) {
            problems.Add(new SnapshotProblem(project, file, e.Message));
        }
    }
}
