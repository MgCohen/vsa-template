namespace Umpire.CheckModel;

public static class ReferenceCheck {
    const string RootType = "System.Object";

    public static void Run(AppModel app, IReadOnlyList<AppType> types, Report report) {
        foreach (var type in types) {
            if (app.PlacementOf(type) is { } placement && Rules.IsChecked(placement)) {
                DeniedUses(app, type, placement, report);
                WideExposures(app, type, placement, report);
                TypePlaces(app, type, placement, report);
            }
            if (app.PlacementOf(type) is { IsExternal: false } subject) {
                MatchCheck.Run(app, type, subject, report);
            }
        }
        CountCheck.Run(app, types, report);
    }

    static void DeniedUses(AppModel app, AppType type, Placement from, Report report) {
        foreach (var use in Uses(type)) {
            if (Other(app, from, use.Key) is not { } to) {
                continue;
            }
            var decision = app.DecisionFor(from, to.Placement);
            if (decision.Permission != Permission.Deny) {
                continue;
            }
            var why = decision.Rule is null
                ? $"no rule allows it (DefaultPermission = {decision.Default})"
                : $"the rule \"{decision.Rule}\" denies it";
            var first = use.First();
            Rules.Add(app, report, RuleCodes.DeniedUse,
                $"{type.Name.Name} in {from.Where} uses {to.Name.Name} in {to.Placement.Where}: {why}. "
                + "Remove the use, or change the rule in Rules.", first.File, first.Fact.Line, first.Fact.Column,
                app.Trust.IsSure(type) && app.Trust.IsSure(use.Key));
        }
    }

    static void WideExposures(AppModel app, AppType type, Placement from, Report report) {
        foreach (var exposure in Exposure.Of(app, type)) {
            if (Other(app, from, exposure.Target) is not { } to) {
                continue;
            }
            var decision = app.DecisionFor(from, to.Placement);
            if (decision is not { Permission: Permission.Allow, Rule: { } rule, Access: { } limit } || exposure.Access <= limit) {
                continue;
            }
            Rules.Add(app, report, RuleCodes.WideExposure,
                $"{type.Name.Name} in {from.Where} exposes {to.Name.Name} in {to.Placement.Where} "
                + $"in {exposure.Member} with {exposure.Access} access, but the rule \"{rule}\" allows {limit} at most. "
                + "Make the member less visible, or change the rule in Rules.", type,
                app.Trust.IsSure(type) && app.Trust.AreSure(TargetsOf(type)));
        }
    }

    static void TypePlaces(AppModel app, AppType type, Placement at, Report report) {
        foreach (var kept in app.Definition.Places.GroupBy(rule => rule.Type)) {
            var rule = kept.First();
            if (kept.Any(r => r.Parts.Contains(at.Part.Position)) || type.Name.FullName == kept.Key.FullName || !Uses(type, rule)) {
                continue;
            }
            var text = string.Join("; ", kept);
            var names = string.Join(" or ", kept.SelectMany(r => r.Parts).Distinct().Select(p => app.Parts[p].Name).Distinct());
            var shown = rule.IsAttribute ? $"[{rule.Shown}]" : rule.Shown;
            Rules.Add(app, report, RuleCodes.TypePlace, rule.IsAttribute
                ? $"{type.Name.Name} in {at.Where} has {shown}, but the rule \"{text}\" keeps it in {names}. "
                    + $"Move the code with {shown} to {names}, or remove the attribute."
                : $"{type.Name.Name} in {at.Where} uses {shown}, but the rule \"{text}\" keeps it in {names}. "
                    + $"Move the code that uses {shown} to {names}, or remove the use.", type, app.Trust.IsSure(type));
        }
    }

    static IEnumerable<IGrouping<TypeName, AppReference>> Uses(AppType type) => type.References
        .Concat(Implied(type.Content).Select(name =>
            new AppReference(type.File, new ReferenceFact(name, ReferenceKind.Interface, type.Line, type.Column))))
        .GroupBy(r => r.Fact.Target);

    public static IEnumerable<TypeName> TargetsOf(AppType type) => type.References.Select(r => r.Fact.Target)
        .Concat(type.Content.BaseArguments)
        .Concat(type.Content.AllInterfaces.SelectMany(i => i.Types))
        .Concat(type.Content.Base?.Types ?? [])
        .Concat(type.Content.Members.SelectMany(m => Exposure.TypesOf(m).Concat(m.Implements)))
        .Distinct();

    internal static IEnumerable<TypeName> Implied(TypeContent content) => content.AllInterfaces.SelectMany(i => i.Types.Take(1))
        .Concat(RootRecord(content))
        .Concat(content.Members.Where(m => m.IsImplicit).SelectMany(Exposure.TypesOf));

    static IEnumerable<TypeName> RootRecord(TypeContent content) => content is { IsRecord: true, Base: not null }
        ? content.Bases.TakeWhile(b => b.FullName != RootType).Skip(1).Reverse().Take(1)
        : [];

    static bool Uses(AppType type, PlaceRule rule) =>
        type.Content.Attributes.Any(a => a.Bases.Any(b => b.FullName == rule.Type.FullName))
        || !rule.IsAttribute && (type.Is(rule.Type) || type.Mentions(rule.Type.FullName));

    static Target? Other(AppModel app, Placement from, TypeName target) =>
        app.TargetOf(target) is { } to && !to.Placement.IsSameInstance(from) ? to : null;
}
