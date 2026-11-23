namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Steps.Domain.Shared;

sealed class MissingReferenceRule : PlaceRule {
    public override int Number => 5;

    public override FindingSeverity Severity => FindingSeverity.Warning;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.MissingReferences.Select(folder => At((string?)null,
            $"The reference folder {folder} does not exist, so the checks do not read its DLLs. "
            + "Make the folder, or remove it from umpire.check.references (--references)."));
}
