namespace BuiltApp.Infrastructure;

using BuiltApp.Domain.Shared;
using Umpire.Architecture;
using Umpire.Config;

public sealed record DefinitionFolder(bool HasSource, bool HasProject) {
    public const string Tests = "Tests";

    public static DefinitionFolder Of(string block, IReadOnlyList<string>? ignoredFolders = null) {
        var files = FolderAliasConfig.Of(UmpireConfig.Find(block), block).DefinitionFolders(block)
            .SelectMany(OnDisk).SelectMany(Files)
            .Where(file => !ReadOptions.IsIgnored(Path.GetRelativePath(block, file), ignoredFolders ?? [])).Select(Path.GetExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new DefinitionFolder(files.Contains(".cs"), files.Contains(".csproj"));
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
