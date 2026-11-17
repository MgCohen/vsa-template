namespace Rules.Infrastructure;

using Rules.Domain;

public static class UmpireConfigFile {
    public const string FileName = ".umpireconfig";

    public static RuleConfig Read(string blockFolder) =>
        Find(blockFolder) is { } file ? new RuleConfig(Values(File.ReadLines(file))) : RuleConfig.Empty;

    public static Dictionary<string, string> Values(IEnumerable<string> lines) {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines.Select(Text).TakeWhile(text => !text.StartsWith('['))) {
            var pair = line.Split('=', 2);
            if (pair.Length == 2 && pair[0].Trim().Length > 0) {
                values[pair[0].Trim()] = pair[1].Trim();
            }
        }
        return values;
    }

    static string Text(string line) => line.Split(';', '#')[0].Trim();

    static string? Find(string blockFolder) {
        for (var folder = new DirectoryInfo(blockFolder); folder is not null; folder = folder.Parent) {
            var file = Path.Combine(folder.FullName, FileName);
            if (File.Exists(file)) {
                return file;
            }
        }
        return null;
    }
}
