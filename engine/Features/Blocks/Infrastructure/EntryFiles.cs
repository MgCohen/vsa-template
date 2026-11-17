namespace Blocks.Infrastructure;

using System.Text.Json;
using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using Umpire.Architecture;

public static class EntryFiles {
    public const string Name = "umpire.json";
    const string ArchitectureProperty = "architecture";

    public static IReadOnlyList<BlockEntry> Above(string folder) {
        var found = new List<BlockEntry>();
        for (var current = new DirectoryInfo(folder); current is not null; current = current.Parent) {
            var file = Path.Combine(current.FullName, Name);
            if (File.Exists(file)) {
                found.Add(Read(file));
            }
        }
        return found;
    }

    public static IReadOnlyList<BlockEntry> Below(string folder) {
        if (!Directory.Exists(folder)) {
            return [];
        }
        return [.. Directory.GetDirectories(folder).Where(IsSource).SelectMany(Tree)
            .Select(sub => Path.Combine(sub, Name))
            .Where(File.Exists)
            .Select(Read)];
    }

    public static BlockEntry Read(string file) {
        var full = DiskPath.Of(file);
        var entry = new BlockEntry(Path.GetDirectoryName(full)!, EntryForm.File, null, full);
        try {
            using var json = JsonDocument.Parse(File.ReadAllText(file));
            if (json.RootElement.ValueKind == JsonValueKind.Object
                && json.RootElement.TryGetProperty(ArchitectureProperty, out var value)
                && value.ValueKind == JsonValueKind.String
                && value.GetString() is { Length: > 0 } name) {
                return TypeName.Parse(name) is { } type ? entry with { Type = type } : entry.Names(name, TypeName.NoAssembly);
            }
            return NoArchitecture(entry, $"it has no string property \"{ArchitectureProperty}\"");
        } catch (JsonException e) {
            return NoArchitecture(entry, $"it is not valid JSON ({e.Message})");
        }
    }

    static BlockEntry NoArchitecture(BlockEntry entry, string reason) =>
        entry with { Problem = $"does not name an architecture: {reason}" };

    static IEnumerable<string> Tree(string folder) =>
        new[] { folder }.Concat(Directory.GetDirectories(folder).Where(IsSource).SelectMany(Tree));

    static bool IsSource(string folder) {
        var name = Path.GetFileName(folder);
        return !BuildOutput.IsFolder(name) && !name.StartsWith('.') && !name.EndsWith('~');
    }
}
