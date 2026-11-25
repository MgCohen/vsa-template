namespace Umpire.CheckModel;

static class CountCheck {
    static readonly object Key = new();

    public static void Run(AppModel app, IReadOnlyList<AppType> types, Report report) {
        var failures = app.Once(Key, () => Failures(app));
        foreach (var type in types) {
            foreach (var failure in failures[type]) {
                Rules.Add(app, report, RuleCodes.Count, failure.Text, type, app.Trust.IsAllSure);
            }
        }
    }

    static ILookup<AppType, CountFailure> Failures(AppModel app) {
        var placed = app.Types.Select(t => app.PlacementOf(t) is { } p ? new PlacedType(t, p) : null).OfType<PlacedType>().ToList();
        return app.Definition.Counts.SelectMany(rule => new CountRun(app, rule, placed).Failures()).ToLookup(f => f.At);
    }
}
