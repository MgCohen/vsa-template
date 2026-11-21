namespace BuiltApp.Domain.Shared;

public static class UnityFolder {
    public const string VersionFile = "ProjectSettings/ProjectVersion.txt";
    public const string ScriptAssemblies = "Library/ScriptAssemblies";

    public static bool Is(string folder) => File.Exists(Path.Combine(folder, VersionFile));

    public static string? Root(string? start) {
        for (var folder = start; !string.IsNullOrEmpty(folder); folder = Path.GetDirectoryName(folder)) {
            if (Is(folder)) {
                return folder;
            }
        }
        return null;
    }
}
