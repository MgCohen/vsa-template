namespace BuiltApp.Infrastructure;

using Umpire.Vsa.Definition;

static class UnityProject {
    public const string ScriptAssembliesFolder = "Library/ScriptAssemblies";

    public static IReadOnlyList<string> Scripts(string blockFolder, IEnumerable<string> loaded, out bool missingScriptAssemblies) {
        missingScriptAssemblies = false;
        if (!UnityLayout.Is(blockFolder)) {
            return [];
        }
        var folder = Path.Combine(blockFolder, ScriptAssembliesFolder);
        if (!Directory.Exists(folder)) {
            missingScriptAssemblies = true;
            return [];
        }
        var names = loaded.Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. Directory.GetFiles(folder, "*.dll")
            .Where(dll => !names.Contains(Path.GetFileNameWithoutExtension(dll)))];
    }
}
