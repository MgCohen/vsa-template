namespace Scaffold.Domain.Shared;

public sealed record ScaffoldInput(string Kind, string Name, IReadOnlyDictionary<string, string> Groups) {
    const char ValueSeparator = '=';
    static readonly char[] PairSeparators = [';', ','];

    public static ScaffoldInput? From(string kind, string name, string groups) =>
        kind.Length == 0 ? null : new ScaffoldInput(kind, name, Parse(groups));

    static Dictionary<string, string> Parse(string groups) =>
        groups.Split(PairSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pair => pair.Split(ValueSeparator, 2, StringSplitOptions.TrimEntries))
            .Where(pair => pair.Length == 2 && pair[0].Length > 0 && pair[1].Length > 0)
            .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.OrdinalIgnoreCase);
}
