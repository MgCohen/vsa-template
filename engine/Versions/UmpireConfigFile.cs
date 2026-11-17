namespace Umpire.Engine.Versions;

public sealed record UmpireConfigFile(string Source, IReadOnlyDictionary<string, string> Values, bool HasSections) {
    public const string Name = ".umpireconfig";
    public const string ArchitectureKey = "umpire.architecture";

    public string? this[string key] => Values.TryGetValue(key, out var value) ? value : null;

    public static UmpireConfigFile Read(string file) => Parse(file, File.ReadAllLines(file));

    public static UmpireConfigFile Parse(string source, IEnumerable<string> lines) {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var hasSections = false;
        foreach (var line in lines.Select(line => line.Trim())) {
            hasSections |= line.StartsWith('[');
            if (!hasSections && IsKeyLine(line, out var equals)) {
                values[line[..equals].Trim()] = line[(equals + 1)..].Trim();
            }
        }
        return new UmpireConfigFile(source, values, hasSections);
    }

    public static string? Nearest(string folder) {
        for (var current = new DirectoryInfo(Path.GetFullPath(folder)); current is not null; current = current.Parent) {
            var file = Path.Combine(current.FullName, Name);
            if (File.Exists(file)) {
                return file;
            }
        }
        return null;
    }

    static bool IsKeyLine(string line, out int equals) {
        equals = line.IndexOf('=');
        return equals > 0 && line[0] is not ('#' or ';');
    }
}
