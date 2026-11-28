namespace CodeCheck.Slices.CheckCode;

sealed class RuleOptions(string ruleId, CodeConfig config, IReadOnlyList<RuleOption> declared) {
    public string? Configured(string key) =>
        config.Value($"umpire.{ruleId}.{key}") ?? config.Value($"umpire.{key}");

    public string? Value(string key) => Configured(key) ?? declared.FirstOrDefault(option => option.Key == key)?.Default;

    public int? Number(string key) => RuleOption.ParseInt(Value(key));

    public bool? Bool(string key) => RuleOption.ParseBool(Value(key));

    public IReadOnlyList<string>? List(string key) => RuleOption.ParseList(Value(key));
}
