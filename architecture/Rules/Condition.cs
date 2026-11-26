namespace Umpire.Architecture;

public sealed class Condition : IEquatable<Condition> {
    readonly IReadOnlyList<string> terms;

    Condition(IReadOnlyList<Part> same, IReadOnlyList<Part> other, IReadOnlyList<string> terms) {
        Same = same;
        Other = other;
        this.terms = terms;
    }

    public static Condition Always { get; } = new([], [], []);

    public IReadOnlyList<Part> Same { get; }
    public IReadOnlyList<Part> Other { get; }

    public bool Equals(Condition? other) => other is not null && Same.SequenceEqual(other.Same) && Other.SequenceEqual(other.Other);

    public override bool Equals(object? obj) => Equals(obj as Condition);

    public override int GetHashCode() => Same.Concat(Other).Aggregate(Same.Count, (hash, part) => unchecked((hash * 31) + part.GetHashCode()));

    public override string ToString() => terms.Count == 0 ? "always" : string.Join(", ", terms);

    internal Condition AndSame(Part group) => Same.Contains(group) ? this : new([.. Same, group], Other, [.. terms, $"same {group.Name}"]);

    internal Condition AndOther(Part group) => new(Same, [.. Other, group], [.. terms, $"other {group.Name}"]);

    internal bool Holds(IReadOnlyDictionary<string, string> from, IReadOnlyDictionary<string, string> to) =>
        Same.All(g => from.ValueOf(g.Name) is { } value && value == to.ValueOf(g.Name))
        && Other.All(g => from.ValueOf(g.Name) is { } value && to.ValueOf(g.Name) is { } other && value != other);
}
