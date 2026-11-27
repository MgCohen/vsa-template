using Tests.Shared;

namespace CheckModel.Tests;

using Blocks.Domain.Shared;
using Blocks.Infrastructure;
using Blocks.Slices.FindBlocks;
using Facts.Domain.Shared;
using Facts.Infrastructure;
using LoadApp.Slices.LoadApp;
using Model.Infrastructure;
using Model.Slices.CheckModel;
using Umpire.Architecture;
using Umpire.CheckModel.Export;
using Umpire.Config;
using UmpireEngine;
using Data = Umpire.CheckModel;
using Finding = Operations.Domain.Shared.Finding;

sealed class DllRun : IDisposable {
    readonly Lazy<Data.AppModel> model;

    DllRun(
        Architecture definition, BlockTree blocks, IReadOnlyList<BinDll> app, IReadOnlyList<BinDll> dlls,
        IReadOnlyList<string> ignored, IReadOnlyList<FolderAlias> aliases, IReadOnlyList<string> resolve) {
        Definition = definition;
        Blocks = blocks;
        App = app;
        Others = [.. dlls.Except(app)];
        Sources = BinIndex.Sources(dlls);
        IgnoredFolders = ignored;
        Config = UmpireConfig.Find(blocks.Current.Folder);
        Aliases = [.. FolderAliasConfig.Of(Config, blocks.Current.Folder).Aliases, .. aliases];
        var source = new DllSource();
        Libraries = [.. source.Read(new DllQuery([.. Others.Where(dll => dll.Types.Count > 0).Select(dll => dll.Path)], resolve))
            .Select(dll => LibraryExport.Of(DllSnapshots.Of([dll], Block)[0], Data.FileMark.Of(Block, dll.Path)))];
        State = Data.StateFolder.At(TempFolder.Create());
        foreach (var snapshot in DllSnapshots.Of(source.Read(new DllQuery([.. app.Select(dll => dll.Path)], resolve)), Block)) {
            State.Save(snapshot);
        }
        foreach (var library in Libraries) {
            State.SaveLibrary(library);
        }
        State.Save(DefinitionExport.Of(new ResolvedArchitecture(definition)), BlockExport.Of(State, Input([.. app.Select(dll => dll.Name)]))
            with { Mode = Data.CheckMode.Error });
        model = new(() => Data.AppModel.Load(State));
    }

    public Architecture Definition { get; }
    public BlockTree Blocks { get; }
    public IReadOnlyList<BinDll> App { get; }
    public IReadOnlyList<BinDll> Others { get; }
    public IReadOnlyDictionary<string, Source> Sources { get; }
    public IReadOnlyList<string> IgnoredFolders { get; }
    public IReadOnlyList<FolderAlias> Aliases { get; }
    public UmpireConfig Config { get; }
    public IReadOnlyList<Data.Snapshot> Libraries { get; }
    public Data.StateFolder State { get; }
    public Data.AppModel Model => model.Value;
    public string Block => Blocks.Current.Folder;

    public static DllRun Of(
        Architecture definition, string bin, string block, IReadOnlyList<string>? ignored = null, IReadOnlyList<FolderAlias>? aliases = null) =>
        Of(bin, new BlockTree(new BlockEntry(block, EntryForm.App, null, block), []), (_, _) => definition, ignored ?? [], aliases ?? []);

    public static DllRun Of(string bin, string project) {
        using var reader = new BinReader(bin);
        var index = reader.Index();
        var above = EntryFiles.Above(project);
        var folder = BlockSearch.Folder(project, "", [.. index.Entries, .. above]);
        IReadOnlyList<BlockEntry> entries = [.. index.Entries, .. above.Concat(EntryFiles.Below(folder!)).Select(reader.Check)];
        var tree = BlockSearch.Find(project, folder, entries, index).Value.Blocks;
        var hasSource = DefinitionFolder.Of(tree.Current.Folder).HasSource;
        return Of(bin, tree, (app, loader) => LoadAppHandler.Definition(tree, app, index.Definitions, loader, hasSource).Value, [], []);
    }

    public IReadOnlyList<Finding> Findings() => [.. Data.ModelCheck.Run(State).Findings.Select(CheckModelHandler.Converted)];

    public BlockInput Input(IReadOnlyList<string> projects) {
        var resolved = new ResolvedArchitecture(Definition);
        return new BlockInput(Block, projects) {
            Libraries = [.. Libraries.Select(library => library.Project)],
            InnerBlocks = Blocks.InnerFolders,
            IgnoredFolders = IgnoredFolders,
            FolderAliases = [.. Aliases.Select(a => new Data.FolderAlias(a.Folder, a.As))],
            DefinitionSources = DefinitionSources.Of(resolved, Block, Sources.ToDictionary(s => s.Key, s => s.Value.File)),
            InputFiles = [.. Config.FilePath is { } config ? new[] { config } : [], .. App.Select(dll => ProjectOf(Block, dll.Name)).Distinct()],
            Config = Config.Values(),
        };
    }

    public void Dispose() => TempFolder.Delete(State.Folder);

    public static string ProjectOf(string block, string name) {
        if (Directory.GetFiles(block, "*.csproj", SearchOption.AllDirectories).FirstOrDefault(file => Builds(file, name)) is { } own) {
            return own;
        }
        var folder = block;
        while (Directory.GetFiles(folder, "*.csproj").Length == 0) {
            folder = Path.GetDirectoryName(folder)!;
        }
        return Directory.GetFiles(folder, "*.csproj")[0];
    }

    static bool Builds(string project, string name) =>
        Path.GetFileNameWithoutExtension(project) == name
        || File.ReadAllText(project).Contains($"<AssemblyName>{name}</AssemblyName>", StringComparison.Ordinal);

    static DllRun Of(
        string bin, BlockTree tree, Func<IReadOnlyList<BinDll>, AppLoader, Architecture> define,
        IReadOnlyList<string> ignored, IReadOnlyList<FolderAlias> aliases) {
        using var reader = new BinReader(bin, new ReadOptions([], null, ignored));
        var index = reader.Index();
        var app = index.AppDlls(tree.Current.Folder, []);
        var definition = define(app, new AppLoader(bin));
        var externals = new ResolvedArchitecture(definition).Parts.Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        return new DllRun(
            definition, tree, app, [.. app.Concat(index.ExternalDlls(externals)).Concat(index.ReferencedDlls(app, [])).Distinct()],
            ignored, aliases, [.. index.Dlls.Select(dll => dll.Path)]);
    }
}
