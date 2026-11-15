namespace Umpire.Vsa.Definition;

using Umpire.Architecture;

static class UnityLayout {
    public const string VersionFile = "ProjectSettings/ProjectVersion.txt";
    public const string AssetsFolder = "Assets";
    public const string PluginsFolder = "Plugins";

    static readonly string AppFolder = new App().Folder;

    static readonly string[] AppMarkers = [new Feature().GroupFolder!, new Plugin().GroupFolder!, new Views().Folder];

    public static bool Is(string appRoot) => File.Exists(Path.Combine(appRoot, VersionFile));

    public static IReadOnlyList<FolderAlias> Aliases(string appRoot, string? unityApp, string unitySource) {
        if (!Is(appRoot)) {
            return [];
        }
        var source = new FolderAlias(unitySource, "");
        if ((unityApp ?? FindApp(appRoot)) is not { } app) {
            return [source];
        }
        var appFolder = $"{AssetsFolder}/{app}";
        return [new FolderAlias(appFolder, ""), new FolderAlias(appFolder, AppFolder), source];
    }

    static string? FindApp(string appRoot) {
        var assets = Path.Combine(appRoot, AssetsFolder);
        if (!Directory.Exists(assets)) {
            return null;
        }
        var found = Directory.GetDirectories(assets)
            .Where(folder => AppMarkers.Any(marker => Directory.Exists(Path.Combine(folder, marker))))
            .Select(Path.GetFileName)
            .Where(name => !string.Equals(name, PluginsFolder, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        return found.Count switch {
            0 => null,
            1 => found[0],
            _ => throw new InvalidOperationException(
                $"The Unity project has {found.Count} app folders in Assets: {string.Join(", ", found)}. "
                + "Set UnityApp in the definition of the app: derive a class from VsaArchitecture and set UnityApp."),
        };
    }
}
