namespace Umpire.Architecture;

internal sealed record NamePattern {
    NamePattern(string prefix, string suffix, bool ofGroup, Type? group) {
        Start = prefix;
        End = suffix;
        OfAGroup = ofGroup;
        Group = group;
    }

    internal string Start { get; init; }
    internal string End { get; init; }
    internal bool OfAGroup { get; init; }
    internal Type? Group { get; init; }

    internal static NamePattern Any { get; } = new("", "", false, null);

    internal NamePattern WithStart(string text) => this with { Start = text };

    internal NamePattern WithEnd(string text) => this with { End = text };

    internal NamePattern AfterGroup(Type? group, string suffix) => this with { OfAGroup = true, Group = group, End = suffix };

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
