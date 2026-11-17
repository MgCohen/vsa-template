namespace Rules.Domain;

public sealed class RuleConfig(IReadOnlyDictionary<string, string> values) {
    public static RuleConfig Empty { get; } = new(new Dictionary<string, string>());

    public RuleValues For(string code) => new(code, values);
}
