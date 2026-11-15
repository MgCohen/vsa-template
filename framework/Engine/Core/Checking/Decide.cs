namespace VSA.Architecture.Engine;

static class Decide {
    public static Decision For(Architecture definition,
        Part fromPart, IReadOnlyDictionary<string, string> fromValues,
        Part toPart, IReadOnlyDictionary<string, string> toValues) {
        var winner = definition.Resolve().LastOrDefault(rule =>
            rule.From == fromPart && rule.To == toPart && Holds(rule.Condition, fromValues, toValues));
        return new Decision(winner?.Permission ?? definition.Options.DefaultPermission, winner);
    }

    static bool Holds(string condition, IReadOnlyDictionary<string, string> from, IReadOnlyDictionary<string, string> to) =>
        condition == "always" || condition.Split(", ").All(term => {
            var words = term.Split(' ', 2);
            var a = from.GetValueOrDefault(words[1]);
            var b = to.GetValueOrDefault(words[1]);
            return words[0] == "same" ? a is not null && a == b : a is not null && b is not null && a != b;
        });
}
