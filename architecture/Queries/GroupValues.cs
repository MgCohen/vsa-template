namespace Umpire.Architecture;

static class GroupValues {
    internal static string? ValueOf(this IReadOnlyDictionary<string, string> values, string group) =>
        values.TryGetValue(group, out var value) ? value : null;
}
