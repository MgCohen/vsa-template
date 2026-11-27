namespace Umpire.Kernels;

using System.Collections.ObjectModel;

public sealed class HostArguments {
    readonly Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

    public HostArguments(IEnumerable<KeyValuePair<string, string>> values) {
        foreach (var pair in values) {
            this.values[pair.Key] = pair.Value;
        }
        All = new ReadOnlyDictionary<string, string>(this.values);
    }

    public static HostArguments Empty { get; } = new([]);

    public IReadOnlyDictionary<string, string> All { get; }

    public string? this[string key] => values.TryGetValue(key, out var value) ? value : null;

    public static HostArguments Read(string[] args) {
        var options = new List<KeyValuePair<string, string>>();
        for (var i = 0; i < args.Length; i++) {
            if (!IsOption(args[i])) {
                continue;
            }
            var name = args[i].Substring(2);
            var equals = name.IndexOf('=');
            if (equals >= 0) {
                Add(options, name.Substring(0, equals), name.Substring(equals + 1));
            } else if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal)) {
                Add(options, name, args[++i]);
            }
        }
        return new HostArguments(options);
    }

    static void Add(List<KeyValuePair<string, string>> options, string key, string value) {
        if (key.Length > 0) {
            options.Add(new KeyValuePair<string, string>(key, value));
        }
    }

    static bool IsOption(string arg) => arg.Length > 2 && arg.StartsWith("--", StringComparison.Ordinal);
}
