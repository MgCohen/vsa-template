namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Operations.Domain.Shared;

sealed class WrongNameRule : PlaceRule {
    public override int Number => 4;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.Resolved.Names(check.Bindings).Select(name => At(name.At.Source!, name.Message));
}
