namespace Umpire.Config;

using System.Globalization;

public sealed class UmpireConfig {
    public const string FileName = ".umpireconfig";
    const char Separator = '.';

    readonly ConfigTable root;

    UmpireConfig(ConfigTable root, string? filePath) {
        this.root = root;
        FilePath = filePath;
    }

    public static UmpireConfig Empty { get; } = new(new ConfigTable(), null);

    public string? FilePath { get; }

    public static UmpireConfig Parse(string toml) => Parse(toml, null);

    public static UmpireConfig In(string? folder) {
        if (string.IsNullOrWhiteSpace(folder)) {
            return Empty;
        }
        var file = Path.Combine(folder, FileName);
        return File.Exists(file) ? Parse(File.ReadAllText(file), Path.GetFullPath(file)) : Empty;
    }

    public static UmpireConfig Find(string folder) {
        for (var current = new DirectoryInfo(Path.GetFullPath(folder)); current is not null; current = current.Parent) {
            var file = Path.Combine(current.FullName, FileName);
            if (File.Exists(file)) {
                return Parse(File.ReadAllText(file), file);
            }
        }
        return Empty;
    }

    public T? Get<T>(string table) where T : class {
        if (Node(table) is not ConfigTable found) {
            return null;
        }
        try {
            return (T)ConfigBinder.Bind(found, typeof(T), table);
        } catch (FormatException e) {
            throw new InvalidOperationException(
                $"The table [{table}] of {FilePath ?? "the TOML text"} does not fit {typeof(T).Name}: {e.Message}", e);
        }
    }

    public string? Value(string path) => Text(Node(path));

    public IEnumerable<KeyValuePair<string, string>> Values() => Leaves(root, "");

    public IEnumerable<KeyValuePair<string, string>> Values(string table) =>
        Node(table) is ConfigTable found ? Leaves(found, table + Separator) : [];

    public IEnumerable<KeyValuePair<string, IReadOnlyList<string>?>> Lists(string table) =>
        Node(table) is ConfigTable found
            ? found.Entries.Select(pair => new KeyValuePair<string, IReadOnlyList<string>?>(
                pair.Key, pair.Value is List<object> items ? items.Select(Text).OfType<string>().ToList() : null))
            : [];

    static UmpireConfig Parse(string toml, string? file) => new(TomlReader.Read(toml, file ?? ""), file);

    static IEnumerable<KeyValuePair<string, string>> Leaves(ConfigTable table, string prefix) {
        foreach (var pair in table.Entries) {
            var key = prefix + pair.Key;
            if (pair.Value is ConfigTable inner) {
                foreach (var leaf in Leaves(inner, key + Separator)) {
                    yield return leaf;
                }
            } else if (Text(pair.Value) is { } text) {
                yield return new(key, text);
            }
        }
    }

    static string? Text(object? value) => value switch {
        string text => text,
        bool flag => flag ? "true" : "false",
        long number => number.ToString(CultureInfo.InvariantCulture),
        ConfigFloat number => ConfigFloat.Format(number.Value),
        _ => null,
    };

    object? Node(string path) {
        object? current = root;
        foreach (var key in path.Split(Separator)) {
            current = current is ConfigTable table ? table.Find(key) : null;
        }
        return current;
    }
}
