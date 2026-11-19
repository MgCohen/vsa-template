namespace Umpire.Vsa.Analyzers;

using System;
using System.Collections.Generic;

public sealed class ConfigValues {
    public const string FileName = ".umpireconfig";

    readonly Dictionary<string, string> values;

    ConfigValues(IEnumerable<KeyValuePair<string, string>> pairs) {
        values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in pairs) {
            if (!values.ContainsKey(pair.Key)) {
                values.Add(pair.Key, pair.Value);
            }
        }
    }

    public static ConfigValues Empty { get; } = new([]);

    public int Count => values.Count;

    public static ConfigValues Parse(string toml) => new(ConfigReader.Values(toml));

    public string? Value(string path) => values.TryGetValue(path, out var value) ? value : null;
}
