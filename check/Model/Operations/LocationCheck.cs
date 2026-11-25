namespace Umpire.CheckModel;

public static class LocationCheck {
    public static void Run(AppModel app, IReadOnlyList<AppType> types, Report report) {
        foreach (var type in types) {
            if (type.IsUnmapped) {
                Rules.Add(app, report, RuleCodes.UnmappedType, $"{type.Name.Name} is in a folder that the definition does not have. "
                    + "Move it to a part, or add the folder in Define.", type, app.Trust.IsSure(type));
            } else if (app.PlacementOf(type) is { } placement && IsWrong(type, placement)) {
                Rules.Add(app, report, RuleCodes.WrongType, $"{type.Name.Name} is in {placement.Where}, but it is not "
                    + $"{string.Join(" or ", placement.Part.Types.Select(t => t.Name))}. Move it, or change its base type.", type, app.Trust.IsSure(type));
            }
        }
    }

    static bool IsWrong(AppType type, Placement placement) => Rules.IsChecked(placement) && !placement.FollowsUser
        && placement.Part.Types.Count > 0 && !placement.Part.Types.Any(type.Is);
}
