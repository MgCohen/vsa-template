namespace Rules.Domain;

public sealed class RuleConfig(IReadOnlyDictionary<string, string> values) {
    public static RuleConfig Empty { get; } = new(new Dictionary<string, string>());

    public static RuleConfig Of(IEnumerable<KeyValuePair<string, string>> values) {
        var all = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in values) {
            all[pair.Key] = pair.Value;
        }
        return new(all);
    }

    public RuleValues For(string code) => new(code, values);
}
