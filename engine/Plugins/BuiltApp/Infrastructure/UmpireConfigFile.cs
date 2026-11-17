namespace BuiltApp.Infrastructure;

public static class UmpireConfigFile {
    public const string FileName = ".umpireconfig";

    static readonly char[] Separators = ['=', ':'];

    public static IReadOnlyDictionary<string, string> Read(string blockFolder) =>
        Find(blockFolder) is { } file ? Values(File.ReadLines(file)) : new Dictionary<string, string>();

    public static IReadOnlyDictionary<string, string> Values(IEnumerable<string> lines) {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines.Select(Text).TakeWhile(text => !text.StartsWith('['))) {
            var separator = line.IndexOfAny(Separators);
            if (separator > 0 && line[..separator].Trim().Length > 0) {
                values[line[..separator].Trim()] = line[(separator + 1)..].Trim();
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
