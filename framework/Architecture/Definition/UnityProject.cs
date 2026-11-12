namespace VSA.Architecture;

public static class UnityProject {
    public const string VersionFile = "ProjectSettings/ProjectVersion.txt";
    public const string LibraryFolder = "Library";
    public const string ScriptAssembliesFolder = "Library/ScriptAssemblies";
    public const string AssetsFolder = "Assets";
    public const string PluginsFolder = "Plugins";

    public static bool Is(string appRoot) => File.Exists(Path.Combine(appRoot, VersionFile));

    public static string ScriptAssemblies(string appRoot) => Path.Combine(appRoot, ScriptAssembliesFolder);

    public static bool IsInLibrary(string relativePath) =>
        relativePath.Replace('\\', '/').StartsWith(LibraryFolder + "/", StringComparison.OrdinalIgnoreCase);
}
