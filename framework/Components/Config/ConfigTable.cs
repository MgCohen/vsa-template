namespace Umpire.Config;

sealed class ConfigTable {
    readonly List<KeyValuePair<string, object>> entries = [];
    readonly Dictionary<string, KeyValuePair<string, object>> index = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<KeyValuePair<string, object>> Entries => entries;

    public bool FromHeader { get; set; }

    public bool FromDottedKey { get; set; }

    public bool IsInline { get; set; }

    public object? Find(string key) => index.TryGetValue(key, out var entry) ? entry.Value : null;

    public string? Spelling(string key) => index.TryGetValue(key, out var entry) ? entry.Key : null;

    public void Add(string key, object value) {
        var entry = new KeyValuePair<string, object>(key, value);
        index.Add(key, entry);
        entries.Add(entry);
    }
}
