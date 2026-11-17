namespace BuiltApp.Infrastructure;

using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using Umpire.Architecture;

public static class AppReader {
    public static LoadedApp Read(
        IReadOnlyList<string> dlls, IReadOnlyDictionary<string, Source> sources, string blockFolder, ResolvedArchitecture architecture) {
        var unity = UnityProject.Scripts(blockFolder, dlls, out var missingScriptAssemblies);
        var files = unity.Concat(dlls).ToList();
        var types = Distinct(Load(files), files);
        var facts = TypeFactsReader.Read(types, WithUnity(BinReader.Sources(unity), sources), architecture);
        return new LoadedApp(blockFolder, files, types, facts, missingScriptAssemblies);
    }

    static Dictionary<string, Source> WithUnity(IReadOnlyDictionary<string, Source> unity, IReadOnlyDictionary<string, Source> sources) {
        var all = new Dictionary<string, Source>(unity);
        foreach (var source in sources) {
            all.TryAdd(source.Key, source.Value);
        }
        return all;
    }

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
            .Where(t => order.Contains(NameOf(t)))
            .OrderBy(t => order.IndexOf(NameOf(t)))
            .DistinctBy(t => t.FullName)];
    }

    static string NameOf(IType type) => type.Assembly.Name.Split(',')[0];
}
