namespace Rules.Slices.CheckRules;

using Umpire.Architecture;
using Umpire.Config;

sealed record FolderAliasConfig(IReadOnlyList<FolderAlias> Aliases, IReadOnlyList<string> BadKeys) {
    public const string Key = "umpire.check.folder_aliases";
    const string CurrentFolder = "./";
    const string BlockRoot = ".";

    public static FolderAliasConfig None { get; } = new([], []);

    public static FolderAliasConfig Of(UmpireConfig config, string blockFolder) {
        if (config.FilePath is not { } file || !IsBlockFolder(Path.GetDirectoryName(file)!, blockFolder)) {
            return None;
        }
        var aliases = new List<FolderAlias>();
        var bad = new List<string>();
        foreach (var pair in config.Lists(Key)) {
            if (pair.Value is null) {
                bad.Add(pair.Key);
                continue;
            }
            aliases.AddRange(pair.Value.Select(target => new FolderAlias(Normal(pair.Key), Normal(target))));
        }
        return new FolderAliasConfig(aliases, bad);
    }

    public static string Normal(string folder) {
        var path = folder.Replace('\\', '/');
        while (path.StartsWith(CurrentFolder, StringComparison.Ordinal)) {
            path = path[CurrentFolder.Length..];
        }
        path = path.Trim('/');
        return path == BlockRoot ? "" : path;
    }

    static bool IsBlockFolder(string configFolder, string blockFolder) =>
        string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(configFolder)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(blockFolder)),
            StringComparison.OrdinalIgnoreCase);
}
