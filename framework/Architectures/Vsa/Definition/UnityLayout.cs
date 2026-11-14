namespace VSA.Architecture.Vsa;

static class UnityLayout {
    public const string VersionFile = "ProjectSettings/ProjectVersion.txt";
    public const string AssetsFolder = "Assets";
    public const string PluginsFolder = "Plugins";

    public static bool Is(string appRoot) => File.Exists(Path.Combine(appRoot, VersionFile));
}
