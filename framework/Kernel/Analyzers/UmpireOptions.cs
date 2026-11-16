namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

// The options of one rule in one file. A rule key wins over a global key: umpire.UMP010201.check_responses, then umpire.check_responses.
// No getter has a default: the values are in Umpire.Rules.globalconfig. A getter gives null if the value of this file is not valid,
// for example from a bad .editorconfig line. Then the rule does not check the file.
public sealed class UmpireOptions(string ruleId, AnalyzerConfigOptions options) {
    public string? Value(string key) =>
        options.TryGetValue($"umpire.{ruleId}.{key}", out var rule) ? rule
        : options.TryGetValue($"umpire.{key}", out var global) ? global
        : null;

    public int? Int(string key) => int.TryParse(Value(key), out var value) && value > 0 ? value : null;

    public bool? Bool(string key) => bool.TryParse(Value(key), out var value) ? value : null;

    // A list value: the items have a comma or a semicolon between them. An empty value gives no items.
    public IReadOnlyList<string>? List(string key) =>
        Value(key)?.Split(',', ';').Select(item => item.Trim()).Where(item => item.Length > 0).ToArray();
}
