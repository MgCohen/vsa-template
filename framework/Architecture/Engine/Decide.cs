using System.Runtime.CompilerServices;

namespace VSA.Architecture.Engine;

public static class Decide {
    static readonly ConditionalWeakTable<ArchitectureDefinition, ILookup<PartPair, PartRule>> RulesByParts = [];

    public static Decision For(ArchitectureDefinition definition, Binding from, Binding to) =>
        For(definition, from.Part, from.Values, to.Part, to.Values);

    public static Decision For(ArchitectureDefinition definition,
        Part fromPart, IReadOnlyDictionary<string, string> fromValues, Part toPart, IReadOnlyDictionary<string, string> toValues) {
        var rules = RulesByParts.GetValue(definition, d => d.Resolve().ToLookup(r => new PartPair(r.From, r.To)));
        PartRule? winner = null;
        foreach (var rule in rules[new PartPair(fromPart, toPart)]) {
            if (Holds(rule.Condition, fromValues, toValues)) {
                winner = rule;
            }
        }
        return new Decision(winner?.Permission ?? definition.Options.DefaultPermission, winner);
    }

    readonly record struct PartPair(Part From, Part To);

    static bool Holds(string condition, IReadOnlyDictionary<string, string> from, IReadOnlyDictionary<string, string> to) =>
        condition == "always" || condition.Split(", ").All(term => {
            var words = term.Split(' ', 2);
            var a = from.GetValueOrDefault(words[1]);
            var b = to.GetValueOrDefault(words[1]);
            return words[0] == "same" ? a is not null && a == b : a is not null && b is not null && a != b;
        });
}
