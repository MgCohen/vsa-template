namespace Baseline.Infrastructure;

using Baseline.Domain;

public static class BaselineFile {
    public const string Name = "umpire-baseline.txt";

    public static string PathIn(string blockFolder) => Path.Combine(blockFolder, Name);

    public static IReadOnlyList<string> Read(string path) => File.Exists(path) ? File.ReadAllLines(path) : [];

    public static void Write(string path, IEnumerable<BaselineEntry> entries) {
        var lines = entries.Select(entry => entry.ToString()).Order(StringComparer.Ordinal).ToList();
        if (lines.Count == 0) {
            File.Delete(path);
            return;
        }
        File.WriteAllText(path, string.Concat(lines.Select(line => line + "\n")));
    }
}
