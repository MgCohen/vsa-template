using ArchUnitNET.Domain;

namespace VSA.Architecture.Engine;

sealed class RuleChecks(ArchitectureDefinition definition, Bindings bindings) {
    public IReadOnlyList<RuleFinding> Of(Binding from) =>
        [.. WrongType(from), .. DeniedUses(from), .. WideExposures(from)];

    public RuleFinding Unmapped(UnmappedType unmapped) =>
        Error("VSA2102", unmapped.Source, $"{unmapped.Type.Name} is in a folder that the definition does not have. "
            + "Move it to a part, or add the folder in Define.");

    static RuleFinding Error(string code, Source source, string text) =>
        new(code, RuleSeverity.Error, source.File, source.Line, source.Column, text);

    IEnumerable<RuleFinding> WrongType(Binding from) {
        var type = from.Type;
        if (from.FollowsUser || from.Part.Types.Count == 0 || from.Part.Types.Any(t => Binder.Is(type, t))) {
            yield break;
        }
        yield return Error("VSA2101", from.Source!, $"{type.Name} is in {from.Where}, but it is not "
            + $"{string.Join(" or ", from.Part.Types.Select(t => t.Name))}. Move it, or change its base type.");
    }

    IEnumerable<RuleFinding> DeniedUses(Binding from) {
        var type = from.Type;
        var targets = type.Dependencies.Select(d => d.Target).Distinct();
        foreach (var to in targets.Select(target => Other(from, target)).OfType<Binding>()) {
            var decision = DecisionFor(from, to);
            if (decision.Permission != Permission.Deny) {
                continue;
            }
            var why = decision.Rule is null
                ? "no rule allows it (DefaultPermission = Deny)"
                : $"the rule \"{decision.Rule.Text}\" denies it";
            yield return Error("VSA2001", from.Source!,
                $"{type.Name} in {from.Where} uses {to.Type.Name} in {to.Where}: {why}. "
                + "Remove the use, or change the rule in Rules.");
        }
    }

    IEnumerable<RuleFinding> WideExposures(Binding from) {
        var type = from.Type;
        foreach (var exposure in Exposures.Of(type, IsFramework).DistinctBy(e => new { e.Target, e.Member })) {
            if (Other(from, exposure.Target) is not { } to) {
                continue;
            }
            var decision = DecisionFor(from, to);
            if (decision.Permission != Permission.Allow || decision.Rule is not { } rule
                || exposure.Access.IsWithin(rule.Access)) {
                continue;
            }
            yield return Error("VSA2003", from.Source!,
                $"{type.Name} in {from.Where} exposes {to.Type.Name} in {to.Where} "
                + $"in {exposure.Member} with {exposure.Access} access, "
                + $"but the rule \"{rule.Text}\" allows {rule.Access} at most. "
                + "Make the member less visible, or change the rule in Rules.");
        }
    }

    bool IsFramework(IType type) => bindings.Of(type)?.External == true;

    Decision DecisionFor(Binding from, Binding to) =>
        Decide.For(definition, from.Part, from.Values, to.Part, to.Values);

    Binding? Other(Binding from, IType target) =>
        bindings.Of(target) is { } to && !(to.Part == from.Part && to.Path == from.Path) ? to : null;
}
