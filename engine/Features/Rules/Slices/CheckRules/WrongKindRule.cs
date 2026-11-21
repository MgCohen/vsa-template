namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Steps.Domain.Shared;

sealed class WrongKindRule : StatementRule {
    public override int Number => 3;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.Resolved.Kinds(check.Bindings).Select(kind => At(kind.At.Source!, kind.Message));
}
