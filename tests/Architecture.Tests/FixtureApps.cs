namespace EngineCore.Tests;

using System.Runtime.CompilerServices;
using Umpire.Architecture;
using Blocks.Domain.Shared;
using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using Rules.Domain;
using Rules.Slices.CheckRules;
using Steps.Domain.Shared;

static class FixtureApps {
    public static readonly string Root = FixtureRoot();

    static readonly Lazy<ArchitectureCheck> GoodMap = new(() => Load(new GoodArchitecture(), AppContext.BaseDirectory, Block("GoodApp")));
    static readonly Lazy<ArchitectureCheck> BadMap = new(() => Load(new BadArchitecture(), AppContext.BaseDirectory, Block("BadApp")));

    public static ArchitectureCheck Good => GoodMap.Value;

    public static ArchitectureCheck Bad => BadMap.Value;

    public static string Block(string app) => Path.Combine(Root, app);

    public static ArchitectureCheck Load(Architecture definition, string bin, string blockFolder, ReadOptions? options = null) {
        var externals = new ResolvedArchitecture(definition).Parts.Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        using var reader = new BinReader(bin, options);
        var index = reader.Index();
        var dlls = index.AppDlls(blockFolder, []).Concat(index.ExternalDlls(externals)).Distinct().ToList();
        var blocks = new BlockTree(new BlockEntry(blockFolder, EntryForm.App, null, blockFolder), []);
        return ArchitectureCheck.Load(
            definition, [.. dlls.Select(d => d.Path)], BinIndex.Sources(dlls), blocks, RuleConfig.Empty, options?.IgnoredFolders);
    }

    public static string Line(Finding finding) {
        var file = Path.GetRelativePath(Root, finding.File!).Replace('\\', '/');
        return $"{file}({finding.Line}): error {finding.Code}: {finding.Message}";
    }

    static string FixtureRoot([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "Fixtures", "Architecture"));
}
