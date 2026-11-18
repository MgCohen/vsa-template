using Tests.Shared;

namespace Rules.Tests;

sealed class RulesConfig {
    RulesConfig(string path) {
        Path = path;
        Text = File.ReadAllText(path);
    }

    public static RulesConfig Kernel { get; } =
        new(Repo.Path("framework", "Kernel", "Analyzers", "Umpire.Kernels.Analyzers.globalconfig"));

    public static RulesConfig Vsa { get; } =
        new(Repo.Path("packs", "Vsa", "Tests", "Analyzers", "Umpire.Vsa.Analyzers.globalconfig"));

    public static IReadOnlyList<RulesConfig> All { get; } = [Kernel, Vsa];

    public string Path { get; }

    public string Text { get; }

    public string FileName => System.IO.Path.GetFileName(Path);

    public string OptionText => string.Join('\n', Text.Split('\n').Where(line => !IsSeverity(line)));

    public IReadOnlyList<string> Keys =>
        [.. Pairs().Select(pair => pair[0].Trim()).Where(key => key is not "is_global" and not "global_level")];

    public static string Default(Umpire.Vsa.Analyzers.UmpireAnalyzerRule rule, string key) =>
        rule.Options.Single(option => option.Key == key).Default!;

    public string? TryValue(string key) =>
        Pairs().Where(pair => pair[0].Trim() == key).Select(pair => pair[1].Trim()).SingleOrDefault();

    static bool IsSeverity(string line) => line.StartsWith("dotnet_diagnostic.", StringComparison.Ordinal);

    IEnumerable<string[]> Pairs() => Text.Split('\n')
        .Where(line => !line.TrimStart().StartsWith('#'))
        .Select(line => line.Split('=', 2))
        .Where(pair => pair.Length == 2);
}
