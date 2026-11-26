namespace Umpire.Config;

sealed class ConfigTable {
    readonly List<KeyValuePair<string, object>> entries = [];
    readonly Dictionary<string, object> index = new(StringComparer.Ordinal);

    public IReadOnlyList<KeyValuePair<string, object>> Entries => entries;

    public bool FromHeader { get; set; }

    public bool FromDottedKey { get; set; }

    public bool IsInline { get; set; }

    public object? Get(string key) => index.TryGetValue(key, out var value) ? value : null;

    public object? Find(string key) =>
        entries.FirstOrDefault(pair => string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)).Value;

    public void Add(string key, object value) {
        index.Add(key, value);
        entries.Add(new KeyValuePair<string, object>(key, value));
    }
}
