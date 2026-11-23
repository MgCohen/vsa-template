namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Steps.Domain.Shared;

sealed class UnmappedTypeRule : PlaceRule {
    public override int Number => 2;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.Bindings.Unmapped.Select(unmapped => At(unmapped.Source,
            $"{unmapped.Type.Name} is in a folder that the definition does not have. "
            + "Move it to a part, or add the folder in Define."));
}
