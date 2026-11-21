namespace Umpire.Config;

using System.Globalization;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Serialization;

public sealed class UmpireConfig {
    public const string FileName = ".umpireconfig";
    const char Separator = '.';

    static readonly TomlSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    readonly TomlTable root;

    UmpireConfig(TomlTable root, string? filePath) {
        this.root = root;
        FilePath = filePath;
    }

    public static UmpireConfig Empty { get; } = new(new TomlTable(), null);

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

    public T? Get<T>(string table, TomlSerializerContext? context = null) where T : class {
        if (Node(table) is not TomlTable found) {
            return null;
        }
        var text = TomlSerializer.Serialize(found, Options);
        try {
            return context is null ? TomlSerializer.Deserialize<T>(text, Options) : TomlSerializer.Deserialize<T>(text, context);
        } catch (TomlException e) {
            throw new InvalidOperationException(
                $"The table [{table}] of {FilePath ?? "the TOML text"} does not fit {typeof(T).Name}: {e.Message}", e);
        }
    }

    public string? Value(string path) => Text(Node(path));

    public IEnumerable<KeyValuePair<string, string>> Values() => Leaves(root, "");

    public IEnumerable<KeyValuePair<string, string>> Values(string table) =>
        Node(table) is TomlTable found ? Leaves(found, table + Separator) : [];

    public IEnumerable<KeyValuePair<string, IReadOnlyList<string>?>> Lists(string table) =>
        Node(table) is TomlTable found
            ? found.Select(pair => new KeyValuePair<string, IReadOnlyList<string>?>(
                pair.Key, pair.Value is TomlArray array ? array.Select(Text).OfType<string>().ToList() : null))
            : [];

    static UmpireConfig Parse(string toml, string? file) {
        var options = new TomlSerializerOptions { SourceName = file ?? "" };
        return new(TomlSerializer.Deserialize<TomlTable>(toml, options) ?? new TomlTable(), file);
    }

    static object? Child(TomlTable table, string key) =>
        table.FirstOrDefault(pair => string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)).Value;

    static IEnumerable<KeyValuePair<string, string>> Leaves(TomlTable table, string prefix) {
        foreach (var pair in table) {
            var key = prefix + pair.Key;
            if (pair.Value is TomlTable inner) {
                foreach (var leaf in Leaves(inner, key + Separator)) {
                    yield return leaf;
                }
            } else if (Text(pair.Value) is { } text) {
                yield return new(key, text);
            }
        }
    }

    static string? Text(object? value) => value switch {
        null or TomlTable or TomlArray or TomlTableArray => null,
        string text => text,
        bool flag => flag ? "true" : "false",
        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    object? Node(string path) {
        object? current = root;
        foreach (var key in path.Split(Separator)) {
            current = current is TomlTable table ? Child(table, key) : null;
        }
        return current;
    }
}
