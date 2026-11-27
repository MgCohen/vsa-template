namespace Umpire.CheckModel;

public static class ModelCheck {
    public static Report Run(StateFolder state, CheckMode? mode = null) {
        var report = new Report();
        report.Describe("state", state.Folder);
        try {
            var settings = state.ReadBlock();
            var targets = settings.Targets;
            if (targets.Count < 2) {
                Run(AppModel.Load(state, settings, null, mode), report);
                return report;
            }
            report.Describe("targets", string.Join(", ", targets));
            foreach (var target in targets) {
                var one = new Report();
                Run(AppModel.Load(state, settings, null, mode, target), one);
                report.Join(one);
            }
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
        if (!report.IsComplete) {
            report.Add(new Finding(DataCodes.NotComplete, Severity.Warning,
                "The check is not complete: some data is old or missing, so a finding can be wrong or missing. Build all projects, and run the check again."));
        }
    }
}
