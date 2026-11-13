using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

namespace VSA.Architecture.Engine;

public static class ArchitectureLoader {
    public static LoadedApp Load(IEnumerable<string> dlls, string appRoot) {
        var root = Path.GetFullPath(appRoot);
        var given = dlls.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var unity = UnityProject.Scripts(root, given);
        var files = unity.Files.Concat(given).ToList();
        var architecture = Architecture(files);
        var types = Distinct(architecture, files);
        var warnings = unity.Warning is null ? new List<RuleFinding>() : [unity.Warning];
        return new LoadedApp(architecture, root, files, types, SourceFiles.Read(files), warnings);
    }

    static ArchUnitNET.Domain.Architecture Architecture(IReadOnlyList<string> files) {
        var loader = new ArchLoader();
        foreach (var file in files) {
            loader.LoadFilteredDirectory(Path.GetDirectoryName(file)!, Path.GetFileName(file), SearchOption.TopDirectoryOnly);
        }
        return loader.Build();
    }

    static List<IType> Distinct(ArchUnitNET.Domain.Architecture architecture, IReadOnlyList<string> files) {
        var order = files.Select(Path.GetFileNameWithoutExtension).OfType<string>()
            .Select((name, index) => new AssemblyOrder(name, index))
            .GroupBy(a => a.Name).ToDictionary(g => g.Key, g => g.Min(a => a.Index));
        return [.. architecture.Types
            .Where(t => order.ContainsKey(NameOf(t)))
            .GroupBy(t => t.FullName)
            .Select(copies => copies.OrderBy(t => order[NameOf(t)]).First())];
    }

    static string NameOf(IType type) => type.Assembly.Name.Split(',')[0];

    sealed record AssemblyOrder(string Name, int Index);
}
