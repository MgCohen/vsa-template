namespace Umpire.CheckModel;

public sealed record TraitRule(IReadOnlyList<RuleEntry> Subjects, IReadOnlyList<int> Parts, Trait Condition) {
    public override string ToString() => $"{RuleEntry.Text(Subjects)} types are {Condition}";
}
