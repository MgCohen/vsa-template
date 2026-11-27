namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Operations.Domain.Shared;
using Umpire.Architecture;

sealed class WideExposureRule : UseRule {
    public override int Number => 2;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.CheckedBindings.SelectMany(from => WideExposures(check, from));

    IEnumerable<Finding> WideExposures(ArchitectureCheck check, Binding from) {
        var type = from.Type;
        foreach (var exposure in type.Exposures) {
            if (Other(check, from, exposure.Target) is not { } to) {
                continue;
            }
            var decision = check.Resolved.DecisionFor(from, to);
            if (decision.Permission != Permission.Allow || decision.Rule is not { } rule
                || exposure.Access.IsWithin(rule.Access)) {
                continue;
            }
            yield return At(from.Source!,
                $"{type.Name} in {from.Where} exposes {to.Type.Name} in {to.Where} "
                + $"in {exposure.Member} with {exposure.Access} access, "
                + $"but the rule \"{rule.Text}\" allows {rule.Access} at most. "
                + "Make the member less visible, or change the rule in Rules.");
        }
    }
}
