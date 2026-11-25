namespace Umpire.CheckModel;

static class Rules {
    public static void Add(AppModel app, Report report, string code, string text, AppType at, bool isSure) =>
        Add(app, report, code, text, at.File, at.Line, at.Column, isSure);

    public static void Add(
        AppModel app, Report report, string code, string text, string? file, int line, int column, bool isSure) {
        if (app.Settings.SeverityOf(code, Severity.Error) is not { } severity) {
            return;
        }
        var isCertain = isSure && app.Trust.IsBlockCurrent;
        if (!isCertain) {
            report.Doubt();
        }
        report.Add(new Finding(code, severity == Severity.Error && !isCertain ? Severity.Warning : severity, text, file, line, column));
    }

    public static bool IsChecked(Placement placement) => !placement.IsExternal && !placement.Part.IsBlockPlace;
}
