using System.Globalization;

namespace VSA.Architecture.Data;

public sealed record TypeName(string FullName, string Assembly) {
    public string Display {
        get {
            var nested = FullName.Split('+');
            var outer = nested[0].Substring(nested[0].LastIndexOf('.') + 1);
            return string.Join(".", nested.Skip(1).Prepend(outer));
        }
    }

    public string? Outer => FullName.Contains('+')
        ? $"{FullName.Substring(0, FullName.LastIndexOf('+'))}, {Assembly}"
        : null;

    public string Simple => FullName.Substring(Math.Max(FullName.LastIndexOf('.'), FullName.LastIndexOf('+')) + 1);

    public int Arity {
        get {
            var tick = Simple.IndexOf('`');
            return tick < 0 ? 0 : int.Parse(Simple.Substring(tick + 1), CultureInfo.InvariantCulture);
        }
    }

    public int Depth => FullName.Count(c => c == '+');

    public static TypeName Parse(string name) {
        var comma = name.IndexOf(',');
        return comma < 0
            ? new TypeName(name.Trim(), "")
            : new TypeName(name.Substring(0, comma).Trim(), name.Substring(comma + 1).Trim());
    }
}
