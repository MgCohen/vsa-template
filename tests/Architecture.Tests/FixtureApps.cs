using System.Runtime.CompilerServices;
using VSA.Architecture;
using VSA.Architecture.Engine;

namespace Architecture.Tests;

static class FixtureApps {
    public static readonly string Root = FixtureRoot();

    static readonly Lazy<ArchitectureMap> GoodMap = new(() => Load(new GoodDefinition(), AppContext.BaseDirectory, Root));
    static readonly Lazy<ArchitectureMap> BadMap = new(() => Load(new BadDefinition(), AppContext.BaseDirectory, Root));

    public static ArchitectureMap Good => GoodMap.Value;

    public static ArchitectureMap Bad => BadMap.Value;

    public static ArchitectureMap Load(ArchitectureDefinition definition, string bin, string appRoot) {
        var externals = definition.Parts().Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        var dlls = Directory.GetFiles(bin, "*.dll")
            .Where(dll => SourceFiles.HasSourceUnder(dll, appRoot) || IsExternal(Path.GetFileNameWithoutExtension(dll), externals));
        return ArchitectureMap.Load(definition, dlls, appRoot);
    }

    public static string Line(RuleFinding finding) {
        var file = Path.GetRelativePath(Root, finding.File!).Replace('\\', '/');
        return $"{file}({finding.Line}): error {finding.Code}: {finding.Text}";
    }

    static bool IsExternal(string name, IReadOnlyList<string> externals) =>
        externals.Any(e => name == e || name.StartsWith(e + ".", StringComparison.Ordinal));

    static string FixtureRoot([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "Fixtures", "Architecture"));
}
