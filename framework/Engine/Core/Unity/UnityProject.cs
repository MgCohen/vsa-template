namespace VSA.Architecture.Engine;

static class UnityProject {
    public const string VersionFile = "ProjectSettings/ProjectVersion.txt";
    public const string LibraryFolder = "Library";
    public const string ScriptAssembliesFolder = "Library/ScriptAssemblies";

    public static bool Is(string appRoot) => File.Exists(Path.Combine(appRoot, VersionFile));

    public static bool IsInLibrary(string relativePath) =>
        relativePath.Replace('\\', '/').StartsWith(LibraryFolder + "/", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<string> Scripts(string appRoot, IEnumerable<string> loaded, out RuleFinding? warning) {
        warning = null;
        if (!Is(appRoot)) {
            return [];
        }
        var folder = Path.Combine(appRoot, ScriptAssembliesFolder);
        if (!Directory.Exists(folder)) {
            warning = new RuleFinding("VSA2103", RuleSeverity.Warning, null, 0, 0,
                $"{appRoot} is a Unity project, but it has no {ScriptAssembliesFolder} folder. "
                + "The checks read only the .NET build, so they do not check the code that only Unity compiles. "
                + "Open the project in Unity to compile its scripts, then build again.");
            return [];
        }
        var names = loaded.Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. Directory.GetFiles(folder, "*.dll")
            .Where(dll => !names.Contains(Path.GetFileNameWithoutExtension(dll)))];
    }
}
