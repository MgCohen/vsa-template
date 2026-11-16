namespace Rules.Slices.CheckRules;

using Steps.Domain.Shared;
using Umpire.Architecture;

sealed class RuleChecks(ResolvedArchitecture resolved, Bindings bindings) {
    public static Finding Unmapped(UnmappedType unmapped) =>
        Error("UMP020202", unmapped.Source, $"{unmapped.Type.Name} is in a folder that the definition does not have. "
            + "Move it to a part, or add the folder in Define.");

    public static Finding Covered(BlockPlace place) =>
        new("UMP020401", FindingSeverity.Error,
            $"{place.Part.Name} ({place.Part.Path}) covers the folder {place.InBlock}, "
            + $"but a check entry makes {place.InBlock} another block. Mark the folder with AddBlock<T>() "
            + "or AddBlockGroup<T>() in Define, or remove the check entry of the inner block.", place.Folder);

    public static Finding Empty(BlockPlace place) =>
        new("UMP020402", FindingSeverity.Error,
            $"{place.Part.Name} ({place.Part.Path}) marks the folder {place.InBlock} as the place of a block, "
            + "but no check entry is in it. Add a check entry to the folder, or remove the place from Define.", place.Folder);

    public IReadOnlyList<Finding> Of(Binding from) =>
        [.. WrongType(from), .. DeniedUses(from), .. WideExposures(from)];

    public IReadOnlyList<Finding> Counts() =>
        [.. resolved.Counts(bindings).Select(count => Error("UMP020301", count.At.Source!, count.Message))];

    public IReadOnlyList<Finding> Matches() =>
        [.. resolved.Matches(bindings).Select(match => Error("UMP020302", match.Subject.Source!, match.Message))];

    static Finding Error(string code, Source source, string text) =>
        new(code, FindingSeverity.Error, text, source.File, source.Line);

    static IEnumerable<Finding> WrongType(Binding from) {
        var type = from.Type;
        if (from.FollowsUser || from.Part.Types.Count == 0 || from.Part.Types.Any(type.Is)) {
            yield break;
        }
        yield return Error("UMP020201", from.Source!, $"{type.Name} is in {from.Where}, but it is not "
            + $"{string.Join(" or ", from.Part.Types.Select(t => t.Name))}. Move it, or change its base type.");
    }

    IEnumerable<Finding> DeniedUses(Binding from) {
        var type = from.Type;
        foreach (var to in type.Uses.Select(target => Other(from, target)).OfType<Binding>()) {
            var decision = resolved.DecisionFor(from, to);
            if (decision.Permission != Permission.Deny) {
                continue;
            }
            var why = decision.Rule is null
                ? "no rule allows it (DefaultPermission = Deny)"
                : $"the rule \"{decision.Rule.Text}\" denies it";
            yield return Error("UMP020101", from.Source!,
                $"{type.Name} in {from.Where} uses {to.Type.Name} in {to.Where}: {why}. "
                + "Remove the use, or change the rule in Rules.");
        }
    }

    IEnumerable<Finding> WideExposures(Binding from) {
        var type = from.Type;
        foreach (var exposure in type.Exposures) {
            if (Other(from, exposure.Target) is not { } to) {
                continue;
            }
            var decision = resolved.DecisionFor(from, to);
            if (decision.Permission != Permission.Allow || decision.Rule is not { } rule
                || exposure.Access.IsWithin(rule.Access)) {
                continue;
            }
            yield return Error("UMP020102", from.Source!,
                $"{type.Name} in {from.Where} exposes {to.Type.Name} in {to.Where} "
                + $"in {exposure.Member} with {exposure.Access} access, "
                + $"but the rule \"{rule.Text}\" allows {rule.Access} at most. "
                + "Make the member less visible, or change the rule in Rules.");
        }
    }

    Binding? Other(Binding from, string target) =>
        bindings.Of(target) is { } to && !(to.Part == from.Part && to.Path == from.Path) ? to : null;
}
