namespace Umpire.Architecture;

public sealed record Condition(IReadOnlyList<Part> Same, IReadOnlyList<Part> Other) {
    public static Condition Always { get; } = new([], []);

    public override string ToString() => Same.Count == 0 && Other.Count == 0 ? "always"
        : string.Join(", ", Same.Select(g => $"same {g.Name}").Concat(Other.Select(g => $"other {g.Name}")));

    internal Condition AndSame(Part group) => Same.Contains(group) ? this : this with { Same = [.. Same, group] };

    internal Condition AndOther(Part group) => this with { Other = [.. Other, group] };

    internal bool Holds(IReadOnlyDictionary<string, string> from, IReadOnlyDictionary<string, string> to) =>
        Same.All(g => from.GetValueOrDefault(g.Name) is { } value && value == to.GetValueOrDefault(g.Name))
        && Other.All(g => from.GetValueOrDefault(g.Name) is { } value && to.GetValueOrDefault(g.Name) is { } other && value != other);
}
