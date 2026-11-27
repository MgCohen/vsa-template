namespace Umpire.CheckModel;

using Facts.Infrastructure;
using BlockFolders = Umpire.Architecture.BlockFolders;

public static class StateCheck {
    public static Report Run(StateFolder state, CheckMode? mode = null) => Run(state, new BlockFolderSource(), mode);

    public static Report Run(StateFolder state, IFactSource<BlockFolderQuery, BlockFolders> folders, CheckMode? mode = null) {
        var report = new Report();
        report.Describe("state", state.Folder);
        try {
            var settings = state.ReadBlock();
            var targets = settings.Targets;
            if (targets.Count < 2) {
                Run(AppModels.Load(state, settings, null, mode), folders, report);
                return report;
            }
            report.Describe("targets", string.Join(", ", targets));
            foreach (var target in targets) {
                var one = new Report();
                Run(AppModels.Load(state, settings, null, mode, target), folders, one);
                report.Join(one);
            }
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException or InvalidOperationException) {
            report.Fail(e.Message);
        }
        return report;
    }

    static void Run(AppModel app, IFactSource<BlockFolderQuery, BlockFolders> folders, Report report) =>
        ModelCheck.Run(app, folders.Read(new BlockFolderQuery(app.Block, BlockPlaceCheck.Patterns(app))), report);
}
