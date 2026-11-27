namespace Umpire.CheckModel;

public sealed record PlaceRule(TypeName Type, bool IsAttribute, IReadOnlyList<RuleEntry> Places, IReadOnlyList<int> Parts) {
    public string Shown => Type.Name.Split('`')[0];

    public override string ToString() => $"{Shown} is only in {RuleEntry.Text(Places)}";
}
