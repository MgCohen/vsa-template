namespace BuiltApp.Infrastructure;

using System.Text.RegularExpressions;
using BuiltApp.Domain.Shared;
using Umpire.Vsa.Definition;

static partial class UnityProject {
    public const string ScriptAssembliesFolder = UnityFolder.ScriptAssemblies;
    const string EngineModule = "UnityEngine.CoreModule.dll";
    const string EditorVersionKey = "m_EditorVersion:";
    static readonly string[] PackageFolders = ["Packages", "Library/PackageCache"];

    public static string? Root(string? start) => UnityFolder.Root(start);

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

    public static IReadOnlyList<string> References(string bin) {
        if (Root(Path.GetFullPath(bin)) is not { } root) {
            return [];
        }
        var packages = PackageFolders.Select(folder => Path.Combine(root, folder)).Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.dll", SearchOption.AllDirectories));
        return [.. packages.Concat(EngineDlls(root))];
    }

    static IEnumerable<string> EngineDlls(string root) {
        if (Managed(root) is not { } managed) {
            return [];
        }
        var engine = Path.Combine(managed, "UnityEngine");
        return Directory.EnumerateFiles(engine, "*.dll").Concat(Directory.EnumerateFiles(managed, "UnityEditor*.dll"));
    }

    static string? Managed(string root) =>
        HubManagedFolders(EditorVersion(root)).Concat(HintPathManagedFolders(root))
            .FirstOrDefault(folder => File.Exists(Path.Combine(folder, "UnityEngine", EngineModule)));

    static string? EditorVersion(string root) =>
        File.ReadLines(Path.Combine(root, UnityFolder.VersionFile))
            .FirstOrDefault(line => line.StartsWith(EditorVersionKey, StringComparison.Ordinal))?[EditorVersionKey.Length..].Trim();

    static IEnumerable<string> HubManagedFolders(string? version) {
        if (version is null) {
            yield break;
        }
        var programs = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(programs, "Unity", "Hub", "Editor", version, "Editor", "Data", "Managed");
        yield return Path.Combine("/Applications", "Unity", "Hub", "Editor", version, "Unity.app", "Contents", "Managed");
        yield return Path.Combine(home, "Unity", "Hub", "Editor", version, "Editor", "Data", "Managed");
    }

    static IEnumerable<string> HintPathManagedFolders(string root) =>
        Directory.EnumerateFiles(root, "*.csproj")
            .SelectMany(project => HintPath().Matches(File.ReadAllText(project)).Select(match => match.Groups[1].Value))
            .Where(path => path.EndsWith(EngineModule, StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetDirectoryName(Path.GetDirectoryName(path.Replace('\\', Path.DirectorySeparatorChar)))!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex("<HintPath>([^<]+)</HintPath>")]
    private static partial Regex HintPath();
}
