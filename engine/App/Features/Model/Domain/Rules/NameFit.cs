namespace Umpire.CheckModel;

sealed class NameFit {
    readonly NameRule rule;
    readonly string name;
    readonly string? expected;

    public NameFit(NameRule rule, IReadOnlyList<Part> parts, Placement placement, string typeName) {
        this.rule = rule;
        name = typeName.Split('`')[0];
        var value = rule is { OfAGroup: true, GroupPart: { } group } ? placement.Value(parts[group]) : null;
        expected = value is null ? null : rule.Start + value + rule.End;
    }

    public bool Fits => rule.OfAGroup ? expected is null || name == expected : HasStart && HasEnd;

    public string Fix => expected ?? (HasStart ? "" : rule.Start) + name + (HasEnd ? "" : rule.End);

    public string Text => rule.OfAGroup
        ? $"is {rule.Start}<{rule.Group?.Display ?? "group"}>{rule.End}"
        : string.Join(" and ", new[] {
            rule.Start.Length > 0 ? $"starts with {rule.Start}" : "",
            rule.End.Length > 0 ? $"ends with {rule.End}" : "",
        }.Where(text => text.Length > 0));

    bool HasStart => name.StartsWith(rule.Start, StringComparison.Ordinal);

    bool HasEnd => name.EndsWith(rule.End, StringComparison.Ordinal);
}
