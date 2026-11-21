namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Steps.Domain.Shared;

sealed class AttributePlaceRule : StatementRule {
    public override int Number => 4;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.Resolved.Owners(check.Bindings).Select(owner => At(owner.At.Source!, owner.Message));
}
