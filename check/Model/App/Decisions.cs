namespace Umpire.CheckModel;

public sealed class Decisions {
    readonly IReadOnlyList<Part> parts;
    readonly Dictionary<long, List<RulePair>> pairs = [];

    public Decisions(Definition definition, IReadOnlyList<Part> parts) {
        this.parts = parts;
        foreach (var rule in definition.Uses) {
            foreach (var pair in rule.Pairs) {
                var key = Key(pair.From, pair.To);
                if (!pairs.TryGetValue(key, out var list)) {
                    list = [];
                    pairs.Add(key, list);
                }
                list.Add(new RulePair(rule, pair));
            }
        }
    }

    public Decision Of(
        Part from, IReadOnlyDictionary<string, string> fromValues, Part to, IReadOnlyDictionary<string, string> toValues) {
        var acting = pairs.TryGetValue(Key(from.Position, to.Position), out var list)
            ? list.LastOrDefault(pair => Holds(pair.Pair, fromValues, toValues))
            : null;
        return new Decision(acting?.Rule, acting?.Pair, to.Options.DefaultPermission);
    }

    static long Key(int from, int to) => ((long)from << 32) | (uint)to;

    static string? Value(IReadOnlyDictionary<string, string> values, Part group) =>
        values.TryGetValue(group.Name, out var value) ? value : null;

    bool Holds(UsePair pair, IReadOnlyDictionary<string, string> from, IReadOnlyDictionary<string, string> to) =>
        pair.Same.Select(p => parts[p]).All(g => Value(from, g) is { } value && value == Value(to, g))
        && pair.Other.Select(p => parts[p])
            .All(g => Value(from, g) is { } value && Value(to, g) is { } other && value != other);

    sealed record RulePair(UseRule Rule, UsePair Pair);
}
