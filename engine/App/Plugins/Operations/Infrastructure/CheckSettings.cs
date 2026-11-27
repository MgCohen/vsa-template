namespace Operations.Infrastructure;

using Umpire.Config;

public sealed record CheckSettings(
    IReadOnlyList<string> References, string? SourceRoot, IReadOnlyList<string> IgnoredFolders, bool ProjectChecks, string? State = null) {
    public const string ReferencesKey = "umpire.check.references";
    public const string SourceRootKey = "umpire.check.source_root";
    public const string IgnoredFoldersKey = "umpire.check.ignored_folders";
    public const string ProjectChecksKey = "umpire.check.project_checks";
    public const string StateKey = "umpire.check.state";
    static readonly char[] ListSeparators = [',', ';'];
    static readonly string[] Off = ["false", "0", "no", "off"];

    public static CheckSettings Of(
        string folder, string references, string sourceRoot, string ignoredFolders, bool? projectChecks) {
        var config = Config(folder);
        var root = config.FilePath is { } file ? Path.GetDirectoryName(file)! : Path.GetFullPath(folder);
        var given = Folders(references, Directory.GetCurrentDirectory());
        var ignored = List(ignoredFolders);
        return new CheckSettings(
            given.Count > 0 ? given : Folders(config.Value(ReferencesKey), root),
            RootOfSources(sourceRoot, config.Value(SourceRootKey), root),
            ignored.Length > 0 ? ignored : List(config.Value(IgnoredFoldersKey)),
            projectChecks ?? IsOn(config.Value(ProjectChecksKey)),
            config.Value(StateKey) is { Length: > 0 } state ? Path.GetFullPath(Path.Combine(root, state)) : null);
    }

    static bool IsOn(string? value) => !Off.Contains(value ?? "", StringComparer.OrdinalIgnoreCase);

    static string? RootOfSources(string given, string? configured, string root) =>
        given.Length > 0 ? Path.GetFullPath(given)
            : configured is { Length: > 0 } ? Path.GetFullPath(Path.Combine(root, configured))
            : null;

    static IReadOnlyList<string> Folders(string? list, string root) =>
        [.. List(list).Select(folder => Path.GetFullPath(Path.Combine(root, folder)))];

    static string[] List(string? list) =>
        list is null ? [] : [.. list.Split(ListSeparators).Select(item => item.Trim()).Where(item => item.Length > 0)];

    static UmpireConfig Config(string folder) {
        try {
            return UmpireConfig.Find(folder);
        } catch (Exception e) when (e is not OperationCanceledException) {
            return UmpireConfig.Empty;
        }
    }
}
