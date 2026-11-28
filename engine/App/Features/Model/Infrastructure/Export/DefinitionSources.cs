namespace Umpire.CheckModel.Export;

using Facts.Infrastructure;
using Api = Umpire.Architecture;

public static class DefinitionSources {
    public static IReadOnlyList<FileMark> Of(
        Api.ResolvedArchitecture resolved, string block, IReadOnlyDictionary<string, string> sources) {
        var packed = resolved.Architecture.Packed?.GetType();
        var definitions = Chain(resolved.Architecture.GetType()).Concat(packed is null ? [] : Chain(packed)).ToList();
        var types = definitions
            .Concat(resolved.Parts.SelectMany(p => Chain(p.Concept!.GetType()))).Distinct().ToList();
        var folders = definitions.Select(t => SourceOf(t, sources)).OfType<string>()
            .Select(file => Path.GetDirectoryName(file)!).ToList();
        var files = sources.Values.Select(Path.GetFullPath).Where(file => folders.Any(f => BlockPath.IsUnder(f, file)) && File.Exists(file));
        return [.. types.Select(type => Mark(type, block, sources))
            .Concat(files.Select(file => Mark(file, (packed ?? resolved.Architecture.GetType()).Assembly.Location, block)))
            .Concat(folders.Distinct().Select(folder => DiskFiles.Mark(block, folder)))
            .GroupBy(mark => mark.Path)
            .Select(group => group.FirstOrDefault(mark => mark.Hash == FileMark.Unknown) ?? group.First())
            .OrderBy(mark => mark.Path, StringComparer.Ordinal)];
    }

    static FileMark Mark(Type type, string block, IReadOnlyDictionary<string, string> sources) {
        var dll = type.Assembly.Location;
        if (SourceOf(type, sources) is not { } file) {
            return DiskFiles.Mark(block, dll);
        }
        return Mark(file, dll, block);
    }

    static FileMark Mark(string file, string dll, string block) {
        var mark = DiskFiles.Mark(block, file);
        return File.GetLastWriteTimeUtc(file) > File.GetLastWriteTimeUtc(dll) ? mark with { Hash = FileMark.Unknown, Modified = 0 } : mark;
    }

    static string? SourceOf(Type type, IReadOnlyDictionary<string, string> sources) =>
        sources.TryGetValue(Open(type).FullName!, out var file) && File.Exists(file) ? Path.GetFullPath(file) : null;

    static Type Open(Type type) => type.IsGenericType ? type.GetGenericTypeDefinition() : type;

    static IEnumerable<Type> Chain(Type type) {
        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType) {
            if (current != typeof(Api.Architecture) && current != typeof(Api.Concept)) {
                yield return current;
            }
        }
    }
}
