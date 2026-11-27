namespace CodeCheck.Slices.CheckCode;

using Umpire.Config;

public sealed class CodeConfig {
    readonly Dictionary<string, string> values;

    CodeConfig(IEnumerable<KeyValuePair<string, string>> pairs) {
        values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in pairs) {
            if (!values.ContainsKey(pair.Key)) {
                values.Add(pair.Key, pair.Value);
            }
        }
    }

    public static CodeConfig Empty { get; } = new([]);

    public int Count => values.Count;

    public static CodeConfig Parse(string toml) => toml.Length == 0 ? Empty : new(UmpireConfig.Parse(toml).Values());

    public string? Value(string path) => values.TryGetValue(path, out var value) ? value : null;
}
