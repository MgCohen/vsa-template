namespace Umpire.CheckModel;

using BlockFolders = Umpire.Architecture.BlockFolders;

public static class ModelCheck {
    public static void Run(AppModel app, BlockFolders folders, Report report) {
        report.Describe("definition", app.Definition.Name);
        report.Describe("projects", string.Join(", ", app.Snapshots.Select(s => s.Snapshot.Project)));
        report.Describe("types", app.Types.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        DataCheck.Run(app, report);
        LocationCheck.Run(app, app.Types, report);
        ContentCheck.Run(app, app.Types, report);
        ReferenceCheck.Run(app, app.Types, report);
        BlockPlaceCheck.Run(app, folders, report);
        if (!report.IsComplete) {
            report.Add(new Finding(DataCodes.NotComplete, Severity.Warning,
                "The check is not complete: some data is old or missing, so a finding can be wrong or missing. Build all projects, and run the check again."));
        }
    }
}
