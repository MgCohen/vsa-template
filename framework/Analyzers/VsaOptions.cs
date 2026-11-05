using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// The options of one rule in one file. A rule key wins over a global key: vsa.VSA1001.check_responses, then vsa.check_responses.
public sealed class VsaOptions(string ruleId, AnalyzerConfigOptions options) {
    string? Get(string key) =>
        options.TryGetValue($"vsa.{ruleId}.{key}", out var rule) ? rule
        : options.TryGetValue($"vsa.{key}", out var global) ? global
        : null;

    public bool Get(string key, bool fallback) => bool.TryParse(Get(key), out var value) ? value : fallback;

    public int Get(string key, int fallback) => int.TryParse(Get(key), out var value) && value > 0 ? value : fallback;

    // A list value: the items have a comma or a semicolon between them. An empty or missing key gives no items.
    public IReadOnlyList<string> GetList(string key) =>
        (Get(key) ?? "").Split(',', ';').Select(item => item.Trim()).Where(item => item.Length > 0).ToArray();
}
