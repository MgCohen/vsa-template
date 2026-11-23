namespace Scaffold.Infrastructure;

using System.Text;

public sealed class TokenMap {
    readonly Dictionary<string, string> values = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string> Values => values;

    public static string Kebab(string name) {
        var text = new StringBuilder();
        for (var i = 0; i < name.Length; i++) {
            var c = name[i];
            var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
            if (i > 0 && char.IsUpper(c) && (!char.IsUpper(name[i - 1]) || nextIsLower)) {
                text.Append('-');
            }
            text.Append(char.ToLowerInvariant(c));
        }
        return text.ToString();
    }

    public TokenMap Add(string token, string value) {
        values[token] = value;
        values[Kebab(token)] = Kebab(value);
        return this;
    }

    public string Apply(string text) {
        var longestFirst = values.Keys
            .OrderByDescending(token => token.Length)
            .ThenBy(token => token, StringComparer.Ordinal);
        foreach (var token in longestFirst) {
            text = text.Replace(token, values[token], StringComparison.Ordinal);
        }
        return text;
    }
}
