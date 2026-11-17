namespace Blocks.Infrastructure;

using Blocks.Domain;
using BuiltApp.Infrastructure;
using Umpire.Engine.Versions;

public static class BlockProjects {
    public static IReadOnlyList<BlockProject> In(string folder, IReadOnlyList<string> innerFolders) =>
        [.. EntryFiles.SourceFolders(folder)
            .Where(sub => !innerFolders.Any(inner => IsAtOrUnder(sub, inner)))
            .SelectMany(sub => Directory.GetFiles(sub, "*.csproj"))
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(file => new BlockProject(DiskPath.Of(file), VersionSlots.HasUmpireMainSdk(file)))];

    public static bool HasConfigFile(string folder) => File.Exists(Path.Combine(folder, UmpireConfigFile.FileName));

    static bool IsAtOrUnder(string path, string folder) =>
        string.Equals(path, folder, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
