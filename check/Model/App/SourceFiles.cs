namespace Umpire.CheckModel;

public static class SourceFiles {
    const string ProjectFile = "*.csproj";
    const string ConfigFile = ".umpireconfig";

    static readonly string[] Patterns = ["*.cs", "*.razor"];

    public static IEnumerable<string> ConfigFiles(string block) => Folders(block, _ => true)
        .Select(folder => Path.Combine(folder, ConfigFile)).Where(File.Exists);

    public static bool HasNew(AppModel app) {
        var known = new HashSet<string>(
            app.Loaded.SelectMany(s => s.Snapshot.Files.Select(s.FileOf))
                .Concat(app.Settings.DefinitionSources.Select(m => BlockPath.In(app.Block, m.Path))), StringComparer.OrdinalIgnoreCase);
        var configs = new HashSet<string>(app.Settings.Inputs.Select(m => BlockPath.In(app.Block, m.Path)), StringComparer.OrdinalIgnoreCase);
        try {
            var oldest = app.Saved.Where(s => s.SavedAt is not null).Select(s => s.SavedAt!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            return Folders(app.Block, folder => !app.IsIgnored(Path.Combine(folder, ProjectFile)) && !IsOfOtherProject(folder, known))
                    .SelectMany(folder => Patterns.SelectMany(p => Directory.GetFiles(folder, p)))
                    .Any(file => !known.Contains(BlockPath.Full(file)) && ChangedAt(file) > oldest)
                || ConfigFiles(app.Block).Any(file => !configs.Contains(BlockPath.Full(file)));
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            return true;
        }
    }

    static DateTime ChangedAt(string file) {
        var info = new FileInfo(file);
        return info.CreationTimeUtc > info.LastWriteTimeUtc ? info.CreationTimeUtc : info.LastWriteTimeUtc;
    }

    static IEnumerable<string> Folders(string folder, Func<string, bool> isIn) => new[] { folder }.Concat(Directory.GetDirectories(folder)
        .Where(sub => !BlockPath.IsIgnored(Path.GetFileName(sub)) && isIn(sub)).SelectMany(sub => Folders(sub, isIn)));

    static bool IsOfOtherProject(string folder, HashSet<string> known) =>
        Directory.GetFiles(folder, ProjectFile).Length > 0 && !known.Any(file => BlockPath.IsUnder(folder, file));
}
