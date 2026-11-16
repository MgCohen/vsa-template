using Tests.Shared;

namespace Rules.Tests;

static class RulesConfig {
    public static string Path { get; } = Repo.Path("tools", "sdk", "Sdk", "Umpire.Rules.globalconfig");

    public static string Text { get; } = File.ReadAllText(Path);

    public static string Value(string key) =>
        Text.Split('\n').Select(line => line.Split('=', 2))
            .Single(pair => pair.Length == 2 && pair[0].Trim() == key)[1].Trim();
}
