namespace Umpire.CheckModel;

public sealed record MatchRule(
    IReadOnlyList<RuleEntry> Subjects, IReadOnlyList<int> Parts, TypeName Taken, IReadOnlyList<int> TakenParts,
    IReadOnlyList<int> GroupParts, TypeName? Group = null) {
    public override string ToString() =>
        $"{RuleEntry.Text(Subjects)} takes {Taken.Display}{(Group is null ? "" : $" in same {Group.Display}")}";
}
