namespace Umpire.Engine.Versions;

public static class AppFolder {
    public const string ConfigFile = ".umpireconfig";

    public static string? Find(string start) {
        var folders = Above(start).ToList();
        return folders.FirstOrDefault(folder => File.Exists(Path.Combine(folder, ConfigFile)))
            ?? folders.FirstOrDefault(HasUmpireProject);
    }

    static bool HasUmpireProject(string folder) =>
        Directory.EnumerateFiles(folder, "*.csproj").Any(VersionSlots.UsesUmpireSdk);

    static IEnumerable<string> Above(string start) {
        for (var folder = new DirectoryInfo(Path.GetFullPath(start)); folder is not null; folder = folder.Parent) {
            yield return folder.FullName;
        }
    }
}
