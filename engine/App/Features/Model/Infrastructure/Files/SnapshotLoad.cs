namespace Umpire.CheckModel;

using Facts.Infrastructure;

sealed class SnapshotLoad {
    readonly List<LoadedSnapshot> own = [];
    readonly List<LoadedSnapshot> others = [];
    readonly List<LoadedSnapshot> libraries = [];
    readonly List<SnapshotProblem> problems = [];
    readonly HashSet<string> projects = new(StringComparer.OrdinalIgnoreCase);
    readonly string? target;

    SnapshotLoad(string? target) {
        this.target = target;
    }

    public static SnapshotSet Of(StateFolder state, BlockSettings settings, Snapshot? current, string? target = null) {
        var load = new SnapshotLoad(target ?? current?.Target);
        var root = state.BlockOf(settings);
        if (current is not null) {
            load.projects.Add(current.Project);
            load.own.Add(LoadedSnapshot.Of(root, current));
        }
        foreach (var project in settings.Projects.Where(load.projects.Add)) {
            load.Load(project, state.SnapshotFile(project, settings.SnapshotOf(project, load.target)), root, load.own);
        }
        foreach (var other in settings.OtherStates.Select(folder => StateFolder.At(BlockPath.In(state.Folder, folder)))) {
            load.AddOther(other);
        }
        foreach (var library in settings.Libraries.Where(load.projects.Add)) {
            load.Load(library, state.LibraryFile(library), root, load.libraries);
        }
        return new SnapshotSet(current, load.own, load.others, load.libraries, load.problems);
    }

    void AddOther(StateFolder other) {
        try {
            var settings = BlockSettings.Read(StateFile.Read(other.BlockFile));
            if (settings.Inputs.Any(mark => !DiskFiles.IsCurrent(mark, other.BlockOf(settings)))) {
                problems.Add(new SnapshotProblem(other.Folder, other.BlockFile, "The block file is old.", IsState: true));
            }
            foreach (var project in settings.Projects.Where(projects.Add)) {
                Load(project, other.SnapshotFile(project, settings.SnapshotOf(project, target)), other.BlockOf(settings), others);
            }
        } catch (Exception e) when (e is FormatException or IOException or UnauthorizedAccessException) {
            problems.Add(new SnapshotProblem(other.Folder, other.BlockFile, e.Message, IsState: true));
        }
    }

    void Load(string project, string file, string root, List<LoadedSnapshot> list) {
        try {
            list.Add(LoadedSnapshot.Of(root, SnapshotCache.Read(file)));
        } catch (Exception e) when (e is FormatException or IOException or UnauthorizedAccessException) {
            problems.Add(new SnapshotProblem(project, file, e.Message, IsLibrary: ReferenceEquals(list, libraries)));
        }
    }
}
