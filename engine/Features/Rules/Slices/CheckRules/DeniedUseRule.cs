namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Steps.Domain.Shared;
using Umpire.Architecture;

sealed class DeniedUseRule : UseRule {
    public override int Number => 1;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.CheckedBindings.SelectMany(from => DeniedUses(check, from));

    IEnumerable<Finding> DeniedUses(ArchitectureCheck check, Binding from) {
        var type = from.Type;
        foreach (var to in type.Uses.Select(target => Other(check, from, target)).OfType<Binding>()) {
            var decision = check.Resolved.DecisionFor(from, to);
            if (decision.Permission != Permission.Deny) {
                continue;
            }
            var why = decision.Rule is null
                ? $"no rule allows it (DefaultPermission = {decision.Default})"
                : $"the rule \"{decision.Rule.Text}\" denies it";
            yield return At(from.Source!,
                $"{type.Name} in {from.Where} uses {to.Type.Name} in {to.Where}: {why}. "
                + "Remove the use, or change the rule in Rules.");
        }
    }
}
