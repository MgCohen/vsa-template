namespace Umpire.Architecture;

internal static class ContentCheck {
    public static IReadOnlyList<ContentResult> Names(Bindings bindings) => [.. Checked(bindings).Where(b => !b.Type.IsNested)
        .Where(b => b.Part.NameRule is { } name && !name.Fits(b))
        .Select(b => new ContentResult($"{b.Part.Name} name {b.Part.NameRule}", b,
            $"{b.Type.Name} is in {b.Where}, but the name of a {b.Part.Name} type {b.Part.NameRule}. "
            + $"Rename it to {b.Part.NameRule!.Fix(b)}, or move it."))];

    public static IReadOnlyList<ContentResult> Kinds(IReadOnlyList<KindStatement> statements, Bindings bindings) => [.. statements
        .SelectMany(statement => Checked(bindings).Where(b => !b.Type.IsNested)
            .Where(b => statement.Subjects.Contains(b.Part) && (b.Type.Kind & statement.Kinds) == 0)
            .Select(b => new ContentResult(statement.Rule, b,
                $"{b.Type.Name} is in {b.Where}, but it is a {b.Type.Kind}: the rule \"{statement.Rule}\" "
                + Allowed(statement.Kinds))))];

    public static IReadOnlyList<ContentResult> Owners(IReadOnlyList<OwnerStatement> statements, Bindings bindings) => [.. statements
        .GroupBy(statement => statement.Attribute)
        .SelectMany(owned => Outside(owned.Key, [.. owned], bindings))];

    static IEnumerable<ContentResult> Outside(Type attribute, List<OwnerStatement> owned, Bindings bindings) {
        var places = owned.SelectMany(statement => statement.Subjects).Distinct().ToList();
        var rule = string.Join("; ", owned.Select(statement => statement.Rule));
        var names = string.Join(" or ", places.Select(p => p.Name).Distinct());
        var mark = $"[{attribute.Name}]";
        return bindings.All
            .Where(b => !b.External && !b.Part.IsBlockPlace && !places.Contains(b.Part)
                && b.Type.Attributes.Contains(OpenName(attribute)))
            .Select(b => new ContentResult(rule, b,
                $"{b.Type.Name} in {b.Where} has {mark}, but the rule \"{rule}\" keeps it in {names}. "
                + $"Move the code with {mark} to {names}, or remove the attribute."));
    }

    static string OpenName(Type type) => (type.IsGenericType ? type.GetGenericTypeDefinition() : type).FullName!;

    static string Allowed(TypeKind kinds) => kinds == TypeKind.None
        ? "allows no type. Move it to another part."
        : $"allows only {KindText.Of(kinds)}. Change the type, or move it to another part.";

    static IEnumerable<Binding> Checked(Bindings bindings) =>
        bindings.All.Where(b => !b.External && !b.Part.IsBlockPlace && !b.FollowsUser);
}
