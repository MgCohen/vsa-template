namespace BuiltApp.Infrastructure;

using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using BuiltApp.Domain.Shared;
using Umpire.Architecture;

public static class AppReader {
    public static LoadedApp Read(
        IReadOnlyList<string> dlls, IReadOnlyDictionary<string, Source> sources, string blockFolder,
        ResolvedArchitecture architecture, IReadOnlyList<string>? ignoredFolders = null) {
        var types = Distinct(Load(dlls), dlls);
        var facts = TypeFactsReader.Read(types, sources, architecture, ConstantFields.Of(dlls))
            .Where(fact => !IsIgnored(fact, blockFolder, ignoredFolders ?? []))
            .ToList();
        return new LoadedApp(blockFolder, dlls, types, facts);
    }

    static bool IsIgnored(TypeFacts fact, string blockFolder, IReadOnlyList<string> ignoredFolders) =>
        ignoredFolders.Count > 0 && fact.Source is { } source
        && ReadOptions.IsIgnored(Path.GetRelativePath(blockFolder, Path.GetFullPath(source.File)), ignoredFolders);

    static ArchUnitNET.Domain.Architecture Load(IReadOnlyList<string> files) {
        var loader = new ArchLoader();
        foreach (var file in files) {
            loader.LoadFilteredDirectory(Path.GetDirectoryName(file)!, Path.GetFileName(file), SearchOption.TopDirectoryOnly);
        }
        return loader.Build();
    }

    static List<IType> Distinct(ArchUnitNET.Domain.Architecture architecture, IReadOnlyList<string> files) {
        var order = files.Select(Path.GetFileNameWithoutExtension).OfType<string>().ToList();
        return [.. architecture.Types
            .Where(t => order.Contains(TypeFactsReader.AssemblyName(t)))
            .OrderBy(t => order.IndexOf(TypeFactsReader.AssemblyName(t)))
            .DistinctBy(t => t.FullName)];
    }
}
