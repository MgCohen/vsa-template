namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed class UmpireOptions(string ruleId, AnalyzerConfigOptions options, IReadOnlyList<UmpireOption> declared) {
    public string? Configured(string key) =>
        options.TryGetValue($"umpire.{ruleId}.{key}", out var rule) ? rule
        : options.TryGetValue($"umpire.{key}", out var global) ? global
        : null;

    public string? Value(string key) => Configured(key) ?? declared.FirstOrDefault(option => option.Key == key)?.Default;

    public int? Int(string key) => UmpireOption.ParseInt(Value(key));

    public bool? Bool(string key) => UmpireOption.ParseBool(Value(key));

    public IReadOnlyList<string>? List(string key) => UmpireOption.ParseList(Value(key));
}
