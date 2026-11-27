namespace Umpire.CheckModel;

public static class ContentCheck {
    public static void Run(AppModel app, IReadOnlyList<AppType> types, Report report) {
        foreach (var type in types) {
            if (app.PlacementOf(type) is not { FollowsUser: false } placement || !Rules.IsChecked(placement)) {
                continue;
            }
            if (!type.IsNested) {
                Name(app, type, placement, report);
            }
            Traits(app, type, placement, report);
        }
    }

    static void Name(AppModel app, AppType type, Placement placement, Report report) {
        if (placement.Part.NameRule is not { } rule) {
            return;
        }
        var name = new NameFit(rule, app.Parts, placement, type.Name.Name);
        if (!name.Fits) {
            Rules.Add(app, report, RuleCodes.WrongName,
                $"{type.Name.Name} is in {placement.Where}, but the name of a {placement.Part.Name} type {name.Text}. "
                + $"Rename it to {name.Fix}, or move it.", type, app.Trust.IsSure(type));
        }
    }

    static void Traits(AppModel app, AppType type, Placement placement, Report report) {
        foreach (var rule in app.Definition.Traits.Where(r => r.Parts.Contains(placement.Part.Position)
            && (!type.IsNested || r.Condition.ReadsNestedTypes))) {
            var fit = new TraitFit(type.Content);
            if (!fit.Has(rule.Condition)) {
                Rules.Add(app, report, RuleCodes.WrongTrait,
                    $"{type.Name.Name} is in {placement.Where}, but {fit.Miss(rule.Condition)}, so it breaks the rule "
                    + $"\"{rule}\". Change the type, or move it to another part.", type, app.Trust.IsSure(type));
            }
        }
    }
}
