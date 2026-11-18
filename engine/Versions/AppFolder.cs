namespace Umpire.Engine.Versions;

using Umpire.Config;

public static class AppFolder {
    public static string? Find(string start) {
        var folders = Above(start).ToList();
        return folders.FirstOrDefault(folder => File.Exists(Path.Combine(folder, UmpireConfig.FileName)))
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
