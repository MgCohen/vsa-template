namespace BuiltApp.Infrastructure;

using BuiltApp.Domain.Shared;
using Umpire.Architecture;
using Umpire.Config;

public sealed record DefinitionFolder(bool HasSource, bool HasProject) {
    public const string Tests = "Tests";
    const string TestProject = ".Tests.csproj";
    const string CurrentFolder = ".";
    static readonly string[] LargeFolders = ["node_modules"];
    static readonly EnumerationOptions OneLevel = new() {
        AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System,
    };
    static readonly EnumerationOptions AllLevels = new() { AttributesToSkip = OneLevel.AttributesToSkip, RecurseSubdirectories = true };

    public static DefinitionFolder Of(string block, IReadOnlyList<string>? ignoredFolders = null) {
        var files = FolderAliasConfig.Of(UmpireConfig.Find(block), block).DefinitionFolders(block)
            .SelectMany(OnDisk).SelectMany(Files)
            .Where(file => !ReadOptions.IsIgnored(Path.GetRelativePath(block, file), ignoredFolders ?? [])).Select(Path.GetExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new DefinitionFolder(files.Contains(".cs"), files.Contains(".csproj"));
    }

    public static IReadOnlyList<string> Below(string block, IReadOnlyList<string> ignoredFolders) {
        var own = FolderAliasConfig.Of(UmpireConfig.Find(block), block).DefinitionFolders(block).Select(Path.GetFullPath).ToList();
        return [.. WithSource(block, ignoredFolders).Where(folder => !own.Contains(folder, StringComparer.OrdinalIgnoreCase))];
    }

    public static IReadOnlyList<string> WithSource(string block, IReadOnlyList<string> ignoredFolders) =>
        [.. Nested(Path.GetFullPath(block), Path.GetFullPath(block), ignoredFolders)
            .Where(folder => Directory.EnumerateFiles(folder, "*.cs", AllLevels)
                .Any(file => !IsSkipped(block, Path.GetDirectoryName(file)!, folder, ignoredFolders)))
            .Order(StringComparer.OrdinalIgnoreCase)];

    static IEnumerable<string> Nested(string block, string folder, IReadOnlyList<string> ignoredFolders) =>
        !Directory.Exists(folder) ? [] : Directory.EnumerateDirectories(folder, "*", OneLevel)
            .Where(sub => !IsSkipped(block, sub, folder, ignoredFolders) && !IsOtherRoot(sub))
            .SelectMany(sub => DefinitionType.IsFolderName(Path.GetFileName(sub)) ? [sub] : Nested(block, sub, ignoredFolders));

    static bool IsSkipped(string block, string folder, string top, IReadOnlyList<string> ignoredFolders) =>
        ReadOptions.IsIgnored(Path.GetRelativePath(block, folder) + "/", ignoredFolders)
        || Path.GetRelativePath(top, folder).Split(Path.DirectorySeparatorChar).Any(name =>
            name != CurrentFolder && (BuildOutput.IsIgnored(name) || string.Equals(name, Tests, StringComparison.OrdinalIgnoreCase)
                || LargeFolders.Contains(name, StringComparer.OrdinalIgnoreCase)));

    static bool IsOtherRoot(string folder) {
        var projects = Directory.EnumerateFiles(folder, "*.csproj", OneLevel).ToList();
        return projects.Any(project => project.EndsWith(TestProject, StringComparison.OrdinalIgnoreCase))
            || (projects.Count > 0 && File.Exists(Path.Combine(folder, UmpireConfig.FileName)));
    }

    static IEnumerable<string> OnDisk(string folder) {
        var parent = Path.GetDirectoryName(folder)!;
        return Directory.Exists(parent)
            ? Directory.GetDirectories(parent).Where(sub => IsName(sub, Path.GetFileName(folder)))
            : [];
    }

    static IEnumerable<string> Files(string folder) => Directory.GetFiles(folder).Concat(Directory.GetDirectories(folder)
        .Where(sub => !BuildOutput.IsIgnored(Path.GetFileName(sub)) && !IsName(sub, Tests))
        .SelectMany(Files));

    static bool IsName(string folder, string name) =>
        string.Equals(Path.GetFileName(folder), name, StringComparison.OrdinalIgnoreCase);
}
