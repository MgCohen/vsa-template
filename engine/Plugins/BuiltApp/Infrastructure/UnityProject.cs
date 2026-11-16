namespace BuiltApp.Infrastructure;

using Steps.Domain.Shared;

static class UnityProject {
    public const string VersionFile = "ProjectSettings/ProjectVersion.txt";
    public const string LibraryFolder = "Library";
    public const string ScriptAssembliesFolder = "Library/ScriptAssemblies";

    public static bool Is(string blockFolder) => File.Exists(Path.Combine(blockFolder, VersionFile));

    public static bool IsInLibrary(string relativePath) =>
        relativePath.Replace('\\', '/').StartsWith(LibraryFolder + "/", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<string> Scripts(string blockFolder, IEnumerable<string> loaded, out Finding? warning) {
        warning = null;
        if (!Is(blockFolder)) {
            return [];
        }
        var folder = Path.Combine(blockFolder, ScriptAssembliesFolder);
        if (!Directory.Exists(folder)) {
            warning = new Finding("UMP020203", FindingSeverity.Warning,
                $"{blockFolder} is a Unity project, but it has no {ScriptAssembliesFolder} folder. "
                + "The checks read only the .NET build, so they do not check the code that only Unity compiles. "
                + "Open the project in Unity to compile its scripts, then build again.");
            return [];
        }
        var names = loaded.Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. Directory.GetFiles(folder, "*.dll")
            .Where(dll => !names.Contains(Path.GetFileNameWithoutExtension(dll)))];
    }
}
