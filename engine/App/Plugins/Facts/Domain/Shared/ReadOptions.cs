namespace Facts.Domain.Shared;

public sealed record ReadOptions(
    IReadOnlyList<string> References, string? SourceRoot, IReadOnlyList<string> IgnoredFolders) {
    const string CurrentFolder = "./";

    public static ReadOptions None { get; } = new([], null, []);

    public static bool IsIgnored(string relative, IReadOnlyList<string> ignoredFolders) {
        var path = relative.Replace('\\', '/');
        return ignoredFolders.Any(folder => path.StartsWith(Normal(folder) + "/", StringComparison.OrdinalIgnoreCase));
    }

    static string Normal(string folder) {
        var path = folder.Replace('\\', '/');
        while (path.StartsWith(CurrentFolder, StringComparison.Ordinal)) {
            path = path[CurrentFolder.Length..];
        }
        return path.Trim('/');
    }
}
