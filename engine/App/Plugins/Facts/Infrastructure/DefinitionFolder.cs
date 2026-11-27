namespace Facts.Infrastructure;

using Facts.Domain.Shared;
using Umpire.Utility;
using Umpire.Config;

public sealed record DefinitionFolder(bool HasSource, bool HasProject) {
    public const string Tests = "Tests";
    const string TestProject = ".Tests.csproj";
    const string CurrentFolder = ".";
    static readonly string[] LargeFolders = ["node_modules"];
    static readonly FolderSearch OneLevel = new() {
        AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System,
    };
    static readonly FolderSearch AllLevels = new() { AttributesToSkip = OneLevel.AttributesToSkip, RecurseSubdirectories = true };

    public static DefinitionFolder Of(string block, IReadOnlyList<string>? ignoredFolders = null) {
        var files = FolderAliasConfig.Of(UmpireConfig.Find(block), block).DefinitionFolders(block)
            .SelectMany(OnDisk).SelectMany(Files)
            .Where(file => !ReadOptions.IsIgnored(RelativePath.Of(block, file), ignoredFolders ?? [])).Select(Path.GetExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new DefinitionFolder(files.Contains(".cs"), files.Contains(".csproj"));
    }

    public static IReadOnlyList<string> Below(string block, IReadOnlyList<string> ignoredFolders) {
        var own = FolderAliasConfig.Of(UmpireConfig.Find(block), block).DefinitionFolders(block).Select(Path.GetFullPath).ToList();
        return [.. WithSource(block, ignoredFolders).Where(folder => !own.Contains(folder, StringComparer.OrdinalIgnoreCase))];
    }

    public static IReadOnlyList<string> WithSource(string block, IReadOnlyList<string> ignoredFolders) =>
        [.. Nested(Path.GetFullPath(block), Path.GetFullPath(block), ignoredFolders)
            .Where(folder => AllLevels.Files(folder, "*.cs")
                .Any(file => !IsSkipped(block, Path.GetDirectoryName(file)!, folder, ignoredFolders)))
            .Order(StringComparer.OrdinalIgnoreCase)];

    public static bool IsSkippedSource(string block, string source, IReadOnlyList<string> ignoredFolders) {
        var relative = RelativePath.Of(block, source);
        return ReadOptions.IsIgnored(relative, ignoredFolders)
            || (!relative.StartsWith("..", StringComparison.Ordinal)
                && relative.Split(Path.DirectorySeparatorChar).Contains(Tests, StringComparer.OrdinalIgnoreCase));
    }

    static IEnumerable<string> Nested(string block, string folder, IReadOnlyList<string> ignoredFolders) =>
        !Directory.Exists(folder) ? [] : OneLevel.Folders(folder, "*")
            .Where(sub => !IsSkipped(block, sub, folder, ignoredFolders) && !IsOtherRoot(sub))
            .SelectMany(sub => DefinitionType.IsFolderName(Path.GetFileName(sub)) ? [sub] : Nested(block, sub, ignoredFolders));

    static bool IsSkipped(string block, string folder, string top, IReadOnlyList<string> ignoredFolders) =>
        ReadOptions.IsIgnored(RelativePath.Of(block, folder) + "/", ignoredFolders)
        || RelativePath.Of(top, folder).Split(Path.DirectorySeparatorChar).Any(name =>
            name != CurrentFolder && (BuildOutput.IsIgnored(name) || string.Equals(name, Tests, StringComparison.OrdinalIgnoreCase)
                || LargeFolders.Contains(name, StringComparer.OrdinalIgnoreCase)));

    static bool IsOtherRoot(string folder) {
        var projects = OneLevel.Files(folder, "*.csproj").ToList();
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
