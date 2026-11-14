namespace VSA.Architecture.Data;

public static class DataFiles {
    public const string Extension = ".json";

    static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase) {
        "global", "package", "package-lock", "appsettings", "launchsettings", "tsconfig", "jsconfig", "dotnet-tools",
    };

    public static string Architecture(string architecture) => Part(architecture) + Extension;

    public static string Snapshot(string app, string architecture) => $"{Part(app)}.{Part(architecture)}{Extension}";

    public static bool IsSnapshot(string path) {
        var name = Path.GetFileName(path);
        return name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase)
            && name.Substring(0, name.Length - Extension.Length).Contains('.');
    }

    static string Part(string name) {
        var part = name.Replace('.', '-').ToLowerInvariant();
        return Reserved.Contains(part)
            ? throw new InvalidOperationException(
                $"The name {name} gives the file {part}{Extension}, and other tools use that file name. Use another name.")
            : part;
    }
}
