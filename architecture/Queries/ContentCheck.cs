namespace Umpire.Architecture;

internal static class ContentCheck {
    public static IReadOnlyList<ContentResult> Names(Bindings bindings) => [.. Checked(bindings).Where(b => !b.Type.IsNested)
        .Where(b => b.Part.NameRule is { } name && !name.Fits(b))
        .Select(b => new ContentResult($"{b.Part.Name} name {b.Part.NameRule}", b,
            $"{b.Type.Name} is in {b.Where}, but the name of a {b.Part.Name} type {b.Part.NameRule}. "
            + $"Rename it to {b.Part.NameRule!.Fix(b)}, or move it."))];

    public static IReadOnlyList<ContentResult> Traits(IReadOnlyList<TraitStatement> statements, Bindings bindings) => [.. statements
        .SelectMany(statement => Checked(bindings).Where(b => !b.Type.IsNested)
            .Where(b => statement.Subjects.Contains(b.Part) && !statement.Condition.Fits(b.Type))
            .Select(b => new ContentResult(statement.Rule, b,
                $"{b.Type.Name} is in {b.Where}, but it is not {statement.Condition}, and the rule \"{statement.Rule}\" "
                + "needs that. Change the type, or move it to another part.")))];

    public static IReadOnlyList<ContentResult> Places(IReadOnlyList<PlaceStatement> statements, Bindings bindings) => [.. statements
        .GroupBy(statement => statement.Type)
        .SelectMany(kept => Outside(kept.Key, [.. kept], bindings))];

    static IEnumerable<ContentResult> Outside(Type type, List<PlaceStatement> kept, Bindings bindings) {
        var places = kept.SelectMany(statement => statement.Places).Distinct().ToList();
        var rule = string.Join("; ", kept.Select(statement => statement.Rule));
        var names = string.Join(" or ", places.Select(p => p.Name).Distinct());
        var open = TypeText.OpenName(type);
        var isAttribute = typeof(Attribute).IsAssignableFrom(type);
        var shown = isAttribute ? $"[{TypeText.Of(type)}]" : TypeText.Of(type);
        return bindings.All
            .Where(b => !b.External && !b.Part.IsBlockPlace && !places.Contains(b.Part) && b.Type.FullName != open
                && (isAttribute ? b.Type.Attributes.Contains(open) : Uses(b.Type, open)))
            .Select(b => new ContentResult(rule, b, isAttribute
                ? $"{b.Type.Name} in {b.Where} has {shown}, but the rule \"{rule}\" keeps it in {names}. "
                    + $"Move the code with {shown} to {names}, or remove the attribute."
                : $"{b.Type.Name} in {b.Where} uses {shown}, but the rule \"{rule}\" keeps it in {names}. "
                    + $"Move the code that uses {shown} to {names}, or remove the use."));
    }

    static bool Uses(TypeFacts type, string name) =>
        type.Bases.Contains(name) || type.Attributes.Contains(name) || type.References.Contains(name);

    static IEnumerable<Binding> Checked(Bindings bindings) =>
        bindings.All.Where(b => !b.External && !b.Part.IsBlockPlace && !b.FollowsUser);
}
