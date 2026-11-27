namespace Umpire.CheckModel;

public static class SourceFiles {
    const string ProjectFile = "*.csproj";
    const string ConfigFile = ".umpireconfig";

    static readonly string[] Patterns = ["*.cs", "*.razor"];

    public static IEnumerable<string> ConfigFiles(string block) => Folders(block, _ => true)
        .Select(folder => Path.Combine(folder, ConfigFile)).Where(File.Exists);

    public static IEnumerable<string> ProjectFiles(string block) => Directory.Exists(block)
        ? Folders(block, _ => true).SelectMany(folder => Directory.GetFiles(folder, ProjectFile))
        : [];

    public static IEnumerable<string> In(string folder) => Directory.Exists(folder)
        ? Folders(folder, sub => Directory.GetFiles(sub, ProjectFile).Length == 0).SelectMany(sub => Patterns.SelectMany(p => Directory.GetFiles(sub, p)))
        : [];

    public static bool HasNew(AppModel app) {
        var compiled = new HashSet<string>(
            app.Loaded.SelectMany(s => s.Snapshot.Files.Select(s.FileOf)), StringComparer.OrdinalIgnoreCase);
        var known = new HashSet<string>(
            compiled.Concat(app.Saved.SelectMany(s => s.Snapshot.NotCompiled.Select(f => BlockPath.In(s.Root, f))))
                .Concat(app.Settings.DefinitionSources.Select(m => BlockPath.In(app.Block, m.Path))), StringComparer.OrdinalIgnoreCase);
        var configs = new HashSet<string>(app.Settings.Inputs.Select(m => BlockPath.In(app.Block, m.Path)), StringComparer.OrdinalIgnoreCase);
        try {
            return app.Saved.Count > 0 && Folders(app.Block, folder => !app.IsIgnored(Path.Combine(folder, ProjectFile)) && !IsOfOtherProject(folder, app))
                    .SelectMany(folder => Patterns.SelectMany(p => Directory.GetFiles(folder, p)))
                    .Any(file => !known.Contains(BlockPath.Full(file)))
                || ConfigFiles(app.Block).Any(file => !configs.Contains(BlockPath.Full(file)));
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            return true;
        }
    }

    static IEnumerable<string> Folders(string folder, Func<string, bool> isIn) => new[] { folder }.Concat(Directory.GetDirectories(folder)
        .Where(sub => !BlockPath.IsIgnored(Path.GetFileName(sub)) && isIn(sub)).SelectMany(sub => Folders(sub, isIn)));

    static bool IsOfOtherProject(string folder, AppModel app) => Directory.GetFiles(folder, ProjectFile) is { Length: > 0 } files
        && !app.Loaded.Any(s => files.Any(f => Path.GetFileNameWithoutExtension(f) == s.Snapshot.Project) || IsAllUnder(folder, s));

    static bool IsAllUnder(string folder, LoadedSnapshot snapshot) =>
        snapshot.Snapshot.Files.Count > 0 && snapshot.Snapshot.Files.All(f => BlockPath.IsUnder(folder, snapshot.FileOf(f)));
}
