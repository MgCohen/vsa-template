namespace BuiltApp.Infrastructure;

using BuiltApp.Domain.Shared;
using Umpire.Architecture;
using Umpire.Config;

public sealed record FolderAliasConfig(IReadOnlyList<FolderAlias> Aliases, IReadOnlyList<string> BadKeys) {
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

    public string DefinitionBlock(string blockFolder, string folder) {
        var relative = Normal(Path.GetRelativePath(blockFolder, folder));
        var definition = relative.Length == 0 ? DefinitionType.FolderName : $"{relative}/{DefinitionType.FolderName}";
        return Aliases.Any(a => IsAlias(a, relative, "") || IsAlias(a, definition, DefinitionType.FolderName)
            || (DefinitionType.IsFolderName(a.As) && (relative + "/").StartsWith(a.Folder + "/", StringComparison.OrdinalIgnoreCase)))
            ? blockFolder
            : folder;
    }

    public string? BlockOfAliasedSource(string blockFolder, string source) {
        var relative = Normal(Path.GetRelativePath(blockFolder, source));
        return Aliases.Any(a => DefinitionType.IsFolderName(a.As) && relative.StartsWith(a.Folder + "/", StringComparison.OrdinalIgnoreCase))
            ? blockFolder
            : null;
    }

    public IReadOnlyList<string> DefinitionFolders(string blockFolder) => [
        Path.Combine(blockFolder, DefinitionType.FolderName),
        .. Aliases.Where(a => a.As.Length == 0).Select(a => Path.Combine(blockFolder, a.Folder, DefinitionType.FolderName)),
        .. Aliases.Where(a => DefinitionType.IsFolderName(a.As)).Select(a => Path.Combine(blockFolder, a.Folder))];

    static bool IsAlias(FolderAlias alias, string folder, string target) =>
        string.Equals(alias.Folder, folder, StringComparison.OrdinalIgnoreCase) && string.Equals(alias.As, target, StringComparison.OrdinalIgnoreCase);

    static bool IsBlockFolder(string configFolder, string blockFolder) =>
        string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(configFolder)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(blockFolder)),
            StringComparison.OrdinalIgnoreCase);
}
