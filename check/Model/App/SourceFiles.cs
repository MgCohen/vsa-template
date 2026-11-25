namespace Umpire.CheckModel;

static class SourceFiles {
    const string ProjectFile = "*.csproj";

    static readonly string[] Patterns = ["*.cs", "*.razor"];

    public static bool HasUnknown(AppModel app) {
        var known = new HashSet<string>(
            app.Loaded.SelectMany(s => s.Snapshot.Files.Select(s.FileOf)), StringComparer.OrdinalIgnoreCase);
        try {
            return HasUnknown(app.Block, app, known, isRoot: true);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            return true;
        }
    }

    static bool HasUnknown(string folder, AppModel app, HashSet<string> known, bool isRoot) {
        if (!isRoot && (BlockPath.IsIgnored(Path.GetFileName(folder)) || app.IsIgnored(Path.Combine(folder, ProjectFile))
            || IsOfOtherProject(folder, known))) {
            return false;
        }
        return Patterns.SelectMany(p => Directory.GetFiles(folder, p)).Any(file => !known.Contains(BlockPath.Full(file)))
            || Directory.GetDirectories(folder).Any(sub => HasUnknown(sub, app, known, isRoot: false));
    }

    static bool IsOfOtherProject(string folder, HashSet<string> known) =>
        Directory.GetFiles(folder, ProjectFile).Length > 0 && !known.Any(file => BlockPath.IsUnder(folder, file));
}
