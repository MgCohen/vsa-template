using Tests.Shared;

namespace EngineCore.Tests;

using System.Runtime.CompilerServices;
using Facts.Domain.Shared;
using Facts.Infrastructure;
using Model.Infrastructure;
using Model.Slices.CheckModel;
using Model.Slices.ExportModel;
using Operations.Domain.Shared;
using Umpire.Architecture;
using Umpire.CheckModel;
using Umpire.CheckModel.Export;
using Umpire.Config;
using Model = Umpire.CheckModel;
using Finding = Operations.Domain.Shared.Finding;
using FolderAlias = Umpire.Architecture.FolderAlias;

static class FixtureApps {
    public static readonly string Root = FixtureRoot();

    static readonly Lazy<CheckedApp> GoodMap = new(() => Load(new GoodArchitecture(), AppContext.BaseDirectory, Block("GoodApp")));
    static readonly Lazy<CheckedApp> BadMap = new(() => Load(new BadArchitecture(), AppContext.BaseDirectory, Block("BadApp")));

    public static CheckedApp Good => GoodMap.Value;

    public static CheckedApp Bad => BadMap.Value;

    public static string Block(string app) => Path.Combine(Root, app);

    public static CheckedApp Load(
        Architecture definition, string bin, string blockFolder, ReadOptions? options = null, IReadOnlyList<FolderAlias>? aliases = null,
        IEnumerable<KeyValuePair<string, string>>? config = null) {
        var resolved = new ResolvedArchitecture(definition);
        var externals = resolved.Parts.Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        using var reader = new BinReader(bin, options);
        var index = reader.Index();
        var app = index.AppDlls(blockFolder, []);
        var named = index.ExternalDlls(externals).Except(app).ToList();
        var all = app.Concat(named).Concat(index.ReferencedDlls(app, [])).Distinct().ToList();
        var source = new DllSource();
        var resolve = index.Dlls.Select(d => d.Path).ToList();
        var facts = source.Read(new DllQuery([.. app.Select(d => d.Path)], resolve, options?.SourceRoot));
        var state = StateFolder.At(TempFolder.Create());
        foreach (var snapshot in DllSnapshots.Of(facts, blockFolder)) {
            state.Save(snapshot);
        }
        var libraries = source.Read(new DllQuery([.. named.Select(d => d.Path)], resolve, options?.SourceRoot))
            .Select(dll => LibraryExport.Of(DllSnapshots.Of([dll], blockFolder)[0], FileMark.Of(blockFolder, dll.Path))).ToList();
        foreach (var library in libraries) {
            state.SaveLibrary(library);
        }
        var file = UmpireConfig.Find(blockFolder);
        var configured = FolderAliasConfig.Of(file, blockFolder);
        var input = new BlockInput(blockFolder, [.. app.Select(d => d.Name)]) {
            Libraries = [.. libraries.Select(l => l.Project)],
            IgnoredFolders = options?.IgnoredFolders ?? [],
            FolderAliases = [.. configured.Aliases.Concat(aliases ?? []).Select(a => new Model.FolderAlias(a.Folder, a.As))],
            DefinitionSources = DefinitionSources.Of(resolved, blockFolder, BinIndex.Sources(all).ToDictionary(s => s.Key, s => s.Value.File)),
            InputFiles = [.. file.FilePath is { } path ? new[] { path } : []],
            Config = config ?? file.Values(),
        };
        var settings = BlockExport.Of(state, input) with { Mode = Model.CheckMode.Error };
        state.Save(DefinitionExport.Of(resolved), settings);
        var setup = ExportModelHandler.Setup(options?.References ?? [], settings, configured.BadKeys).ToList();
        return new CheckedApp(state, facts, setup);
    }

    public static string Line(Finding finding) {
        var file = Path.GetRelativePath(Root, finding.File!).Replace('\\', '/');
        return $"{file}({finding.Line}): error {finding.Code}: {finding.Message}";
    }

    static string FixtureRoot([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "Fixtures", "Architecture"));
}
