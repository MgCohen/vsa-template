namespace Umpire.CheckModel;

public static class DataCheck {
    const string Fix = "Build the project again, for example with dotnet build --no-incremental.";

    public static void Run(AppModel app, Report report) {
        foreach (var problem in app.Problems) {
            report.Add(new Finding(DataCodes.NoSnapshot, Severity.Warning, Text(problem)));
        }
        foreach (var snapshot in app.Snapshots) {
            foreach (var file in snapshot.Snapshot.Files.Where(f => !Freshness.IsCurrent(snapshot.FileOf(f), f))) {
                report.Add(new Finding(DataCodes.OldSnapshot, Severity.Warning,
                    $"The snapshot of {snapshot.Snapshot.Project} is old: the file changed after the last build of the project. {Fix}",
                    snapshot.FileOf(file)));
            }
        }
    }

    public static string Text(SnapshotProblem problem) =>
        $"The snapshot of {problem.Project} ({problem.File}) cannot be read: {problem.Reason} "
        + $"The check has no types of {problem.Project}. {Fix}";
}
