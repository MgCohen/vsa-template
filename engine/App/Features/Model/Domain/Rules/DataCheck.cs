namespace Umpire.CheckModel;

public static class DataCheck {
    const string Fix = "Build the project again, for example with dotnet build --no-incremental.";

    public static void Run(AppModel app, Report report) {
        foreach (var problem in app.Problems) {
            Add(report, DataCodes.NoSnapshot, Text(problem), null);
        }
        foreach (var snapshot in app.Saved) {
            foreach (var file in Freshness.OldFiles(snapshot)) {
                Add(report, DataCodes.OldSnapshot,
                    $"The snapshot of {snapshot.Snapshot.Project} is old: the file changed after the last build of the project. {Fix}",
                    file);
            }
        }
        foreach (var library in app.Trust.OldLibraries) {
            Add(report, DataCodes.OldSnapshot,
                Text(library), null);
        }
        if (BlockText(app) is { } text) {
            Add(report, DataCodes.OldSnapshot, text, null);
        }
    }

    public static string? BlockText(AppModel app) => app.Trust.BlockProblem is { } problem
        ? $"The definition or the block file is old: {problem}. Each finding is a warning. Write the files again."
        : null;

    public static string Text(LoadedSnapshot library) =>
        $"The library facts of {library.Snapshot.Project} are old or have no mark of their DLL. Write them again.";

    public static string OldText(string project) => $"The data of {project} is old, so a finding that uses it is a warning. {Fix}";

    public static string Text(SnapshotProblem problem) =>
        $"The facts of {problem.Project} ({problem.File}) cannot be read: {problem.Reason} "
        + $"The check has no types of {problem.Project}. {Fix}";

    static void Add(Report report, string code, string text, string? file) {
        report.Doubt();
        report.Add(new Finding(code, Severity.Warning, text, file));
    }
}
