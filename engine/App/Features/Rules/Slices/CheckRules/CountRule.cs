namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Operations.Domain.Shared;

sealed class CountRule : StatementRule {
    public override int Number => 1;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.Resolved.Counts(check.Bindings).Select(count => At(count.At.Source!, count.Message));
}
