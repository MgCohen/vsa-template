using ArchUnitNET.Domain;

namespace VSA.Architecture.Engine;

public sealed class RuleChecks(ArchitectureDefinition definition, Bindings bindings, string appRoot) {
    public IReadOnlyList<RuleFinding> Of(IType type) {
        if (bindings.Of(type) is not { External: false } from) {
            return bindings.IsUnmapped(type) ? [Unmapped(type)] : [];
        }
        return [.. WrongType(type, from), .. DeniedUses(type, from), .. WideExposures(type, from)];
    }

    RuleFinding Unmapped(IType type) =>
        Error("VSA2102", type, $"{type.Name} is in a folder that the definition does not have. Move it to a part, or add the folder in Define.");

    IEnumerable<RuleFinding> WrongType(IType type, Binding from) {
        if (from.FollowsUser || from.Part.Types.Count == 0 || from.Part.Types.Any(t => Binder.Is(type, t))) {
            yield break;
        }
        yield return Error("VSA2101", type, $"{type.Name} is in {from.Where}, but it is not "
            + $"{string.Join(" or ", from.Part.Types.Select(t => t.Name))}. Move it, or change its base type.");
    }

    IEnumerable<RuleFinding> DeniedUses(IType type, Binding from) {
        foreach (var to in type.Dependencies.Select(d => d.Target).Distinct().Select(target => Other(from, target)).OfType<Binding>()) {
            var decision = Decide.For(definition, from, to);
            if (decision.Permission != Permission.Deny) {
                continue;
            }
            var why = decision.Rule is null
                ? "no rule allows it (DefaultPermission = Deny)"
                : $"the rule \"{decision.Rule.Text}\" denies it";
            yield return Error("VSA2001", type, $"{type.Name} in {from.Where} uses {to.Type.Name} in {to.Where}: {why}. "
                + "Remove the use, or change the rule in Rules.");
        }
    }

    IEnumerable<RuleFinding> WideExposures(IType type, Binding from) {
        foreach (var exposure in Exposures.Of(type, bindings.Framework.Contains).DistinctBy(e => new { e.Target, e.Member })) {
            if (Other(from, exposure.Target) is not { } to) {
                continue;
            }
            var decision = Decide.For(definition, from, to);
            if (decision.Permission != Permission.Allow || decision.Rule is not { } rule || exposure.Access.IsWithin(rule.Access)) {
                continue;
            }
            yield return Error("VSA2003", type, $"{type.Name} in {from.Where} exposes {to.Type.Name} in {to.Where} "
                + $"in {exposure.Member} with {exposure.Access} access, but the rule \"{rule.Text}\" allows {rule.Access} at most. "
                + "Make the member less visible, or change the rule in Rules.");
        }
    }

    Binding? Other(Binding from, IType target) =>
        bindings.Of(target) is { } to && !(to.Part == from.Part && to.Path == from.Path) ? to : null;

    RuleFinding Error(string code, IType type, string text) {
        var source = bindings.SourceOf(type);
        return source is null
            ? new RuleFinding(code, RuleSeverity.Error, null, 0, 0, $"{type.FullName}: {text}")
            : new RuleFinding(code, RuleSeverity.Error, FullPath(source.File), source.Line, source.Column, text);
    }

    string FullPath(string file) => Path.IsPathRooted(file) ? file : Path.GetFullPath(Path.Combine(appRoot, file));
}
