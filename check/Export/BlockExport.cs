namespace Umpire.CheckModel.Export;

using System.Text.RegularExpressions;

public static partial class BlockExport {
    public static BlockSettings Of(
        StateFolder state, string block, IReadOnlyList<string> projects, IReadOnlyList<string> innerBlocks,
        IReadOnlyList<StateFolder> otherStates, IReadOnlyList<string> ignoredFolders, IReadOnlyList<FolderAlias> folderAliases,
        IEnumerable<KeyValuePair<string, string>> config) => new(
            BlockSettings.CurrentFormat,
            Relative(state.Folder, block),
            projects,
            [.. innerBlocks.Select(inner => Relative(block, inner))],
            [.. otherStates.Select(other => Relative(state.Folder, other.Folder))],
            ignoredFolders,
            folderAliases,
            Severities(config));

    static Dictionary<string, string> Severities(IEnumerable<KeyValuePair<string, string>> config) {
        var severities = new Dictionary<string, string>();
        foreach (var pair in config) {
            if (SeverityKey().Match(pair.Key) is { Success: true } key) {
                severities[key.Groups["code"].Value.ToUpperInvariant()] = pair.Value;
            }
        }
        return severities;
    }

    static string Relative(string folder, string path) => Path.GetRelativePath(folder, path).Replace('\\', '/');

    [GeneratedRegex(@"^umpire\.(?<code>UMP\d+)\.severity$", RegexOptions.IgnoreCase)]
    private static partial Regex SeverityKey();
}
