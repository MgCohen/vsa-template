namespace Blocks.Infrastructure;

using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using Umpire.Architecture;
using Umpire.Engine.Versions;

public static class EntryFiles {
    public const string Name = UmpireConfigFile.Name;
    public const string Key = UmpireConfigFile.ArchitectureKey;

    public static IReadOnlyList<BlockEntry> Above(string folder) {
        var found = new List<BlockEntry>();
        for (var current = new DirectoryInfo(folder); current is not null; current = current.Parent) {
            if (Read(Path.Combine(current.FullName, Name)) is { } entry) {
                found.Add(entry);
            }
        }
        return found;
    }

    public static IReadOnlyList<BlockEntry> Below(string folder) =>
        Directory.Exists(folder)
            ? [.. Directory.GetDirectories(folder).Where(IsSource).SelectMany(Tree)
                .Select(sub => Read(Path.Combine(sub, Name))).OfType<BlockEntry>()]
            : [];

    public static BlockEntry? Read(string file) {
        if (!File.Exists(file) || UmpireConfigFile.Read(file)[Key] is not string name) {
            return null;
        }
        var full = DiskPath.Of(file);
        var entry = new BlockEntry(Path.GetDirectoryName(full)!, EntryForm.File, null, full);
        if (name.Length == 0) {
            return entry with { Problem = $"does not name an architecture: the key {Key} has no value" };
        }
        return TypeName.Parse(name) is { } type ? entry with { Type = type } : entry.Names(name, TypeName.NoAssembly);
    }

    public static IEnumerable<string> SourceFolders(string folder) =>
        Directory.Exists(folder) ? Tree(folder) : [];

    static IEnumerable<string> Tree(string folder) =>
        new[] { folder }.Concat(Directory.GetDirectories(folder).Where(IsSource).SelectMany(Tree));

    static bool IsSource(string folder) {
        var name = Path.GetFileName(folder);
        return !BuildOutput.IsFolder(name) && !name.StartsWith('.') && !name.EndsWith('~');
    }
}
