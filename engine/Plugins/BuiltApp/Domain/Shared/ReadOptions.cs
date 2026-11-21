namespace BuiltApp.Domain.Shared;

public sealed record ReadOptions(
    IReadOnlyList<string> References, string? SourceRoot, IReadOnlyList<string> IgnoredFolders) {
    public static ReadOptions None { get; } = new([], null, []);

    public static bool IsIgnored(string relative, IReadOnlyList<string> ignoredFolders) {
        var path = relative.Replace('\\', '/');
        return ignoredFolders.Any(folder =>
            path.StartsWith(folder.Replace('\\', '/').Trim('/') + "/", StringComparison.OrdinalIgnoreCase));
    }
}
