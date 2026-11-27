namespace Umpire.CheckModel;

sealed class FolderPaths(string block, IReadOnlyList<FolderAlias> aliases, IReadOnlyList<string> ignored) {
    const string CurrentFolder = "./";

    readonly IReadOnlyList<string> ignored = [.. ignored.Select(Normal)];

    public bool IsInBlock(string file) => BlockPath.IsUnder(block, file);

    public bool IsRead(string file) =>
        IsInBlock(file) && (aliases.Count == 0 || aliases.Any(alias => Aliased(alias, BlockPath.Of(block, file)) is not null));

    public bool IsIgnored(string file) =>
        ignored.Any(folder => BlockPath.Of(block, file).StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<string> Of(string file) {
        var relative = BlockPath.Of(block, file);
        if (!IsInBlock(file) || BlockPath.IsInIgnoredFolder(relative)) {
            return [];
        }
        return aliases.Count == 0 ? [relative] : [.. aliases.Select(alias => Aliased(alias, relative)).OfType<string>()];
    }

    static string? Aliased(FolderAlias alias, string relative) {
        var folder = alias.Folder.TrimEnd('/');
        if (folder.Length > 0 && !relative.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase)) {
            return null;
        }
        var rest = folder.Length == 0 ? relative : relative.Substring(folder.Length + 1);
        return alias.As.Length == 0 ? rest : $"{alias.As.TrimEnd('/')}/{rest}";
    }

    static string Normal(string folder) {
        var path = folder.Replace('\\', '/');
        while (path.StartsWith(CurrentFolder, StringComparison.Ordinal)) {
            path = path.Substring(CurrentFolder.Length);
        }
        return path.Trim('/');
    }
}
