namespace Umpire.CheckModel;

public sealed record UseRule(
    bool IsSameConcept, IReadOnlyList<RuleEntry> From, Permission Permission, IReadOnlyList<RuleEntry> To, IReadOnlyList<UsePair> Pairs) {
    const string SameConcept = "same concept";

    public override string ToString() => IsSameConcept
        ? SameConcept
        : $"{RuleEntry.Text(From)} {(Permission == Permission.Allow ? "uses" : "denies")} {RuleEntry.Text(To)}";
}
