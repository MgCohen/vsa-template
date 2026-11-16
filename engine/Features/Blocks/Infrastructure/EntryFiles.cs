namespace Blocks.Infrastructure;

using System.Text.Json;
using BuiltApp.Domain.Shared;
using Umpire.Architecture;

public static class EntryFiles {
    public const string Name = "umpire.json";
    const string ArchitectureProperty = "architecture";

    public static IReadOnlyList<EntryFile> Above(string folder) {
        var found = new List<EntryFile>();
        for (var current = new DirectoryInfo(folder); current is not null; current = current.Parent) {
            var file = Path.Combine(current.FullName, Name);
            if (File.Exists(file)) {
                found.Add(Read(file));
            }
        }
        return found;
    }

    public static IReadOnlyList<EntryFile> Below(string folder) {
        if (!Directory.Exists(folder)) {
            return [];
        }
        return [.. Directory.GetDirectories(folder).Where(IsSource).SelectMany(Tree)
            .Select(sub => Path.Combine(sub, Name))
            .Where(File.Exists)
            .Select(Read)];
    }

    public static EntryFile Read(string file) {
        var full = Path.GetFullPath(file);
        var folder = Path.GetDirectoryName(full)!;
        try {
            using var json = JsonDocument.Parse(File.ReadAllText(file));
            if (json.RootElement.ValueKind == JsonValueKind.Object
                && json.RootElement.TryGetProperty(ArchitectureProperty, out var value)
                && value.ValueKind == JsonValueKind.String
                && value.GetString() is { Length: > 0 } name) {
                return new EntryFile(new BlockEntry(folder, EntryForm.File, name, full), null);
            }
            return new EntryFile(new BlockEntry(folder, EntryForm.File, null, full), $"it has no string property \"{ArchitectureProperty}\"");
        } catch (JsonException e) {
            return new EntryFile(new BlockEntry(folder, EntryForm.File, null, full), $"it is not valid JSON ({e.Message})");
        }
    }

    static IEnumerable<string> Tree(string folder) =>
        new[] { folder }.Concat(Directory.GetDirectories(folder).Where(IsSource).SelectMany(Tree));

    static bool IsSource(string folder) {
        var name = Path.GetFileName(folder);
        return !BuildOutput.IsFolder(name) && !name.StartsWith('.') && !name.EndsWith('~');
    }
}
