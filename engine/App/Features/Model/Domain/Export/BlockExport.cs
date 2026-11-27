namespace Umpire.CheckModel.Export;

using System.Text.RegularExpressions;
using Umpire.Utility;

public static class BlockExport {
    static readonly Regex SeverityKey =
        new(@"^umpire\.(?<code>UMP\d+)\.severity$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static BlockSettings Of(StateFolder state, BlockInput input) => new(
        BlockSettings.CurrentFormat,
        Relative(state.Folder, input.Block),
        ModeOf(input.Config),
        "",
        input.Projects,
        input.Libraries,
        [.. input.InnerBlocks.Select(inner => Relative(input.Block, inner))],
        [.. input.OtherStates.Select(other => Relative(state.Folder, other.Folder))],
        input.IgnoredFolders,
        input.FolderAliases,
        input.DefinitionSources,
        [.. input.InputFiles.Concat(SourceFiles.ConfigFiles(input.Block)).Distinct(StringComparer.OrdinalIgnoreCase).Select(file => FileMark.Of(input.Block, file))],
        Severities(input.Config));

    static CheckMode ModeOf(IEnumerable<KeyValuePair<string, string>> config) =>
        CheckModes.Of(config.LastOrDefault(pair => string.Equals(pair.Key, CheckModes.Key, StringComparison.OrdinalIgnoreCase)).Value);

    static Dictionary<string, string> Severities(IEnumerable<KeyValuePair<string, string>> config) {
        var severities = new Dictionary<string, string>();
        foreach (var pair in config) {
            if (SeverityKey.Match(pair.Key) is { Success: true } key) {
                severities[key.Groups["code"].Value.ToUpperInvariant()] = pair.Value;
            }
        }
        return severities;
    }

    static string Relative(string folder, string path) => RelativePath.Of(folder, path).Replace('\\', '/');
}
