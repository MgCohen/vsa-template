namespace Umpire.Architecture;

public sealed record Name {
    Name(string prefix, string suffix, bool ofGroup, Type? group) {
        Start = prefix;
        End = suffix;
        OfAGroup = ofGroup;
        Group = group;
    }

    internal string Start { get; init; }
    internal string End { get; init; }
    internal bool OfAGroup { get; init; }
    internal Type? Group { get; init; }

    public static Name Prefix(string text) => new(text, "", false, null);

    public static Name Suffix(string text) => new("", text, false, null);

    public static Name OfGroup() => new("", "", true, null);

    public static Name OfGroup<T>() where T : Concept => new("", "", true, typeof(T));

    public Name WithPrefix(string text) => this with { Start = text };

    public Name WithSuffix(string text) => this with { End = text };

    public override string ToString() {
        if (OfAGroup) {
            return $"is {Start}<{(Group is null ? "group" : Concept.Display(Group))}>{End}";
        }
        var parts = new List<string>();
        if (Start.Length > 0) {
            parts.Add($"starts with {Start}");
        }
        if (End.Length > 0) {
            parts.Add($"ends with {End}");
        }
        return string.Join(" and ", parts);
    }

    internal static string Plain(string typeName) => typeName.Split('`')[0];

    internal Part? GroupOf(Part part) =>
        !OfAGroup ? null : Group is null ? part.Groups.FirstOrDefault() : part.Groups.FirstOrDefault(g => g.Is(Group));

    internal string? Expected(Binding binding) {
        if (!OfAGroup) {
            return null;
        }
        return GroupOf(binding.Part) is { } group && binding.Values.TryGetValue(group.Name, out var value)
            ? Start + value + End
            : null;
    }

    internal bool Fits(Binding binding) {
        var name = Plain(binding.Type.Name);
        if (OfAGroup) {
            return Expected(binding) is not { } expected || name == expected;
        }
        return name.StartsWith(Start, StringComparison.Ordinal) && name.EndsWith(End, StringComparison.Ordinal);
    }

    internal string Fix(Binding binding) {
        if (Expected(binding) is { } expected) {
            return expected;
        }
        var name = Plain(binding.Type.Name);
        var start = name.StartsWith(Start, StringComparison.Ordinal) ? "" : Start;
        var end = name.EndsWith(End, StringComparison.Ordinal) ? "" : End;
        return start + name + end;
    }
}
