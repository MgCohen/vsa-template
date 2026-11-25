namespace Umpire.CheckModel;

static class MatchCheck {
    public static void Run(AppModel app, AppType type, Placement subject, Report report) {
        if (subject.FollowsUser || type.Content.IsAbstract || type.Content.IsStatic) {
            return;
        }
        foreach (var rule in app.Definition.Matches.Where(m => m.Parts.Contains(subject.Part.Position))) {
            Check(app, rule, type, subject, report);
        }
    }

    static void Check(AppModel app, MatchRule rule, AppType type, Placement subject, Report report) {
        var isSure = app.Trust.IsSure(type) && app.Trust.AreSure(type.Content.BaseArguments);
        var taken = type.Content.BaseArguments.Select(app.TargetOf).OfType<Target>()
            .Where(t => !t.Placement.IsExternal && rule.TakenParts.Contains(t.Placement.Part.Position)).Distinct().ToList();
        if (taken.Count == 0) {
            Rules.Add(app, report, RuleCodes.Match,
                $"{type.Name.Name} in {subject.Where} takes no {rule.Taken.Display} type: the rule \"{rule}\" needs one. "
                + $"Give its base type a {rule.Taken.Display} type argument, or move it.", type, isSure);
            return;
        }
        if (rule.Group is not { } concept) {
            return;
        }
        var group = concept.Display;
        var own = subject.Part.Groups.FirstOrDefault(g => rule.GroupParts.Contains(g.Position));
        foreach (var other in taken.Where(t => own is null || !IsSameInstance(own, subject, t.Placement))) {
            Rules.Add(app, report, RuleCodes.Match,
                $"{type.Name.Name} in {subject.Where} takes {other.Name.Name} in {other.Placement.Where}: "
                + $"the rule \"{rule}\" needs both in one {group}. "
                + $"Move {type.Name.Name} to the {group} of {other.Name.Name}, or take a type of its own {group}.", type, isSure);
        }
    }

    static bool IsSameInstance(Part group, Placement subject, Placement other) =>
        group.Holds(other.Part) && group.Groups.All(g => subject.Value(g) == other.Value(g));
}
