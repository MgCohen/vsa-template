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

    public static IReadOnlyList<string> Below(string block, IReadOnlyList<string> ignoredFolders) {
        var own = FolderAliasConfig.Of(UmpireConfig.Find(block), block).DefinitionFolders(block);
        return [.. Nested(block)
            .Where(folder => !own.Contains(folder, StringComparer.OrdinalIgnoreCase)
                && Files(folder).Any(file => IsSource(file)
                    && !ReadOptions.IsIgnored(Path.GetRelativePath(block, file), ignoredFolders)))
            .Order(StringComparer.OrdinalIgnoreCase)];
    }

    static IEnumerable<string> Nested(string folder) => Directory.GetDirectories(folder)
        .Where(sub => !BuildOutput.IsIgnored(Path.GetFileName(sub)) && !IsName(sub, Tests) && !IsOtherRoot(sub))
        .SelectMany(sub => DefinitionType.IsFolderName(Path.GetFileName(sub)) ? [sub] : Nested(sub));

    static bool IsOtherRoot(string folder) =>
        File.Exists(Path.Combine(folder, UmpireConfig.FileName)) && Directory.GetFiles(folder, "*.csproj").Length > 0;

    static bool IsSource(string file) => string.Equals(Path.GetExtension(file), ".cs", StringComparison.OrdinalIgnoreCase);

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
