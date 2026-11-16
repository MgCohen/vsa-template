using Tests.Shared;

namespace Rules.Tests;

// Umpire.Rules.globalconfig: the one place of the VSA rule values. The analyzer tests use this file, so they check the real values.
static class RulesConfig {
    public static string Path { get; } = Repo.Path("tools", "sdk", "Sdk", "Umpire.Rules.globalconfig");

    public static string Text { get; } = File.ReadAllText(Path);

    // The value of one key, for example Value("umpire.UMP010101.max").
    public static string Value(string key) =>
        Text.Split('\n').Select(line => line.Split('=', 2))
            .Single(pair => pair.Length == 2 && pair[0].Trim() == key)[1].Trim();
}
