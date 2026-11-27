namespace Umpire.CheckModel;

public sealed record RuleEntry(
    TypeName Concept, IReadOnlyList<RuleFilter> Filters, Access? Access = null, Depth? Depth = null, Boundary? Boundary = null) {
    public static string Text(IReadOnlyList<RuleEntry> entries) => string.Join(" and ", entries);

    public override string ToString() => string.Join(" ", new[] { Named() }.Concat(Filters.Select(f => f.ToString())));

    string Named() {
        var options = new object?[] { Access, Depth, Boundary }.OfType<object>().ToList();
        return options.Count == 0 ? Concept.Display : $"{Concept.Display}({string.Join(", ", options)})";
    }
}
