namespace Umpire.CheckModel;

static class Rules {
    public static void Add(AppModel app, Report report, string code, string text, AppType at) =>
        Add(app, report, code, text, at.File, at.Line, at.Column);

    public static void Add(AppModel app, Report report, string code, string text, string? file, int line = 1, int column = 1) {
        if (app.Settings.SeverityOf(code, Severity.Error) is { } severity) {
            report.Add(new Finding(code, severity, text, file, line, column));
        }
    }

    public static bool IsChecked(Placement placement) => !placement.IsExternal && !placement.Part.IsBlockPlace;

    public static bool IsCheckedAlone(AppType type, Placement placement) =>
        IsChecked(placement) && !placement.FollowsUser && !type.IsNested;
}
