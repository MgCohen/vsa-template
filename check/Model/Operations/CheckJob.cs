namespace Umpire.CheckModel;

public static class CheckJob {
    public static Report Run(StateFolder state) {
        var report = new Report();
        report.Describe("state", state.Folder);
        try {
            Run(AppModel.Load(state), report);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException or InvalidOperationException) {
            report.Fail(e.Message);
        }
        return report;
    }

    static void Run(AppModel app, Report report) {
        report.Describe("definition", app.Definition.Name);
        report.Describe("projects", string.Join(", ", app.Snapshots.Select(s => s.Snapshot.Project)));
        report.Describe("types", app.Types.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        DataCheck.Run(app, report);
        LocationCheck.Run(app, app.Types, report);
        ContentCheck.Run(app, app.Types, report);
        ReferenceCheck.Run(app, app.Types, report);
        BlockPlaceCheck.Run(app, report);
    }
}
