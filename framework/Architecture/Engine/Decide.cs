using System.Runtime.CompilerServices;

namespace VSA.Architecture.Engine;

public static class Decide {
    static readonly ConditionalWeakTable<ArchitectureDefinition, ILookup<(Part From, Part To), Pair>> PairsByParts = [];

    public static (Access Access, Pair? Pair) For(ArchitectureDefinition definition, Binding from, Binding to) =>
        For(definition, from.Part, from.Items, to.Part, to.Items);

    public static (Access Access, Pair? Pair) For(ArchitectureDefinition definition,
        Part fromPart, IReadOnlyDictionary<string, string> fromItems, Part toPart, IReadOnlyDictionary<string, string> toItems) {
        var pairs = PairsByParts.GetValue(definition, d => d.Resolve().ToLookup(p => (p.From, p.To)));
        Pair? winner = null;
        foreach (var pair in pairs[(fromPart, toPart)]) {
            if (Holds(pair.Condition, fromItems, toItems)) {
                winner = pair;
            }
        }
        return (winner?.Access ?? definition.Options.Unlisted, winner);
    }

    static bool Holds(string condition, IReadOnlyDictionary<string, string> from, IReadOnlyDictionary<string, string> to) =>
        condition == "always" || condition.Split(", ").All(term => {
            var words = term.Split(' ', 2);
            var a = from.GetValueOrDefault(words[1]);
            var b = to.GetValueOrDefault(words[1]);
            return words[0] == "same" ? a is not null && a == b : a is not null && b is not null && a != b;
        });
}
