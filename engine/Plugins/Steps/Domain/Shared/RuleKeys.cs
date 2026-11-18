namespace Steps.Domain.Shared;

using Umpire.Config;

public static class RuleKeys {
    public static readonly IReadOnlyList<string> Tables = ["umpire", "dotnet_diagnostic"];

    public static IEnumerable<KeyValuePair<string, string>> Of(UmpireConfig config) =>
        Tables.SelectMany(config.Values).Where(pair => !pair.Value.Contains('\n') && !pair.Value.Contains('\r'));
}
