namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Steps.Domain.Shared;

sealed class WrongTypeRule : PlaceRule {
    public override int Number => 1;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.CheckedBindings
            .Where(from => !from.FollowsUser && from.Part.Types.Count > 0 && !from.Part.Types.Any(from.Type.Is))
            .Select(from => At(from.Source!, $"{from.Type.Name} is in {from.Where}, but it is not "
                + $"{string.Join(" or ", from.Part.Types.Select(t => t.Name))}. Move it, or change its base type."));
}
