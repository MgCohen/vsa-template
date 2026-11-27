namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Operations.Domain.Shared;

sealed class MatchRule : StatementRule {
    public override int Number => 2;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.Resolved.Matches(check.Bindings).Select(match => At(match.Subject.Source!, match.Message));
}
