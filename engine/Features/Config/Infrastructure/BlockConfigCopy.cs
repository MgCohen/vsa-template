namespace Config.Infrastructure;

using Steps.Domain.Shared;
using Umpire.Config;

public static class BlockConfigCopy {
    public const string FileName = "Umpire.Block.globalconfig";
    public const string Folder = "artifacts~";
    public const int GlobalLevel = 101;

    public static string Write(UmpireConfig config) {
        var block = Path.GetDirectoryName(config.FilePath!)!;
        var copy = Path.Combine(block, Folder, FileName);
        var text = Text(config);
        if (!File.Exists(copy) || File.ReadAllText(copy) != text) {
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.WriteAllText(copy, text);
        }
        return copy;
    }

    public static string Text(UmpireConfig config) {
        var lines = new[] { "is_global = true", $"global_level = {GlobalLevel}" }
            .Concat(RuleKeys.Of(config).Select(pair => $"{pair.Key} = {pair.Value}"));
        return string.Concat(lines.Select(line => line + "\n"));
    }
}
