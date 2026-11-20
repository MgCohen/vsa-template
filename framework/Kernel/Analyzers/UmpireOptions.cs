namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using System.Linq;

public sealed class UmpireOptions(string ruleId, ConfigValues config, IReadOnlyList<UmpireOption> declared) {
    public string? Configured(string key) =>
        config.Value($"umpire.{ruleId}.{key}") ?? config.Value($"umpire.{key}");

    public string? Value(string key) => Configured(key) ?? declared.FirstOrDefault(option => option.Key == key)?.Default;

    public int? Int(string key) => UmpireOption.ParseInt(Value(key));

    public bool? Bool(string key) => UmpireOption.ParseBool(Value(key));

    public IReadOnlyList<string>? List(string key) => UmpireOption.ParseList(Value(key));
}
