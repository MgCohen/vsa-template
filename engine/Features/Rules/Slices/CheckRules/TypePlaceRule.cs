namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Steps.Domain.Shared;

sealed class TypePlaceRule : StatementRule {
    public override int Number => 4;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.Resolved.Places(check.Bindings).Select(place => At(place.At.Source!, place.Message));
}
