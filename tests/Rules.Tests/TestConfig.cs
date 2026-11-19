namespace Rules.Tests;

using System.Globalization;
using Umpire.Vsa.Analyzers;

static class TestConfig {
    const string UmpirePrefix = "umpire.";

    public static string Default(UmpireAnalyzerRule rule, string key) =>
        rule.Options.Single(option => option.Key == key).Default!;

    public static string Native(string config) =>
        string.Concat(Lines(config).Where(line => !IsUmpire(line)).Select(line => line + "\n"));

    public static string Umpire(string config) {
        var lines = Lines(config).Where(IsUmpire).Select(Toml).ToList();
        var keys = lines.Select(line => line.Split('=')[0].Trim()).ToHashSet(StringComparer.Ordinal);
        var on = Analyzer.AllRules.Where(rule => !rule.IsOnByDefault)
            .Select(rule => $"{UmpirePrefix}{rule.Id}.{RuleSeverity.Key}")
            .Where(key => !keys.Contains(key))
            .Select(key => $"{key} = \"error\"");
        return string.Concat(lines.Concat(on).Select(line => line + "\n"));
    }

    static IEnumerable<string> Lines(string config) =>
        config.Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.Trim().Length > 0);

    static bool IsUmpire(string line) => line.TrimStart().StartsWith(UmpirePrefix, StringComparison.Ordinal);

    static string Toml(string line) {
        var parts = line.Split('=', 2);
        var value = parts[1].Trim();
        return $"{parts[0].Trim()} = {(IsPlain(value) ? value : Quoted(value))}";
    }

    static bool IsPlain(string value) =>
        value is "true" or "false" || long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
        || (value.Length > 1 && value[0] == '"' && value[^1] == '"');

    static string Quoted(string value) =>
        "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
}
