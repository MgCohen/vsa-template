using System.Runtime.CompilerServices;
using Umpire.Architecture;
using BuiltApp.Infrastructure;
using Rules.Slices.CheckRules;
using Steps.Domain.Shared;

namespace EngineCore.Tests;

static class FixtureApps {
    public static readonly string Root = FixtureRoot();

    static readonly Lazy<ArchitectureCheck> GoodMap = new(() => Load(new GoodArchitecture(), AppContext.BaseDirectory, Root));
    static readonly Lazy<ArchitectureCheck> BadMap = new(() => Load(new BadArchitecture(), AppContext.BaseDirectory, Root));

    public static ArchitectureCheck Good => GoodMap.Value;

    public static ArchitectureCheck Bad => BadMap.Value;

    public static ArchitectureCheck Load(Architecture definition, string bin, string appRoot) {
        var externals = definition.Parts().Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        var dlls = Directory.GetFiles(bin, "*.dll")
            .Where(dll => SourceFiles.HasSourceUnder(dll, appRoot) || IsExternal(Path.GetFileNameWithoutExtension(dll), externals));
        return ArchitectureCheck.Load(definition, dlls, appRoot);
    }

    public static string Line(Finding finding) {
        var file = Path.GetRelativePath(Root, finding.File!).Replace('\\', '/');
        return $"{file}({finding.Line}): error {finding.Code}: {finding.Message}";
    }

    static bool IsExternal(string name, IReadOnlyList<string> externals) =>
        externals.Any(e => name == e || name.StartsWith(e + ".", StringComparison.Ordinal));

    static string FixtureRoot([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "Fixtures", "Architecture"));
}
