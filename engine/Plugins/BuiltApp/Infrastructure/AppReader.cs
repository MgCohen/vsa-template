using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using Steps.Domain.Shared;
using VSA.Architecture;

namespace BuiltApp.Infrastructure;

public static class AppReader {
    public static LoadedApp Read(IEnumerable<string> dlls, string appRoot, ResolvedArchitecture architecture) {
        var root = Path.GetFullPath(appRoot);
        var given = dlls.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var unity = UnityProject.Scripts(root, given, out var warning);
        var files = unity.Concat(given).ToList();
        var types = Distinct(Load(files), files);
        var facts = TypeFactsReader.Read(types, SourceFiles.Read(files), architecture);
        var warnings = warning is null ? new List<Finding>() : [warning];
        return new LoadedApp(root, files, types, facts, warnings);
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
