namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Steps.Domain.Shared;

sealed class WrongTraitRule : StatementRule {
    public override int Number => 3;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.Resolved.Traits(check.Bindings).Select(trait => At(trait.At.Source!, trait.Message));
}
