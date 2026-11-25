namespace CheckModel.Tests;

using Blocks.Domain.Shared;
using Blocks.Infrastructure;
using Blocks.Slices.FindBlocks;
using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using LoadApp.Slices.LoadApp;
using Rules.Domain;
using Rules.Slices.CheckRules;
using Steps.Domain.Shared;
using Umpire.Architecture;
using Umpire.Config;

sealed class EngineRun {
    EngineRun(Architecture definition, BlockTree blocks, IReadOnlyList<BinDll> app, IReadOnlyList<BinDll> dlls) {
        Definition = definition;
        Blocks = blocks;
        App = app;
        Others = [.. dlls.Except(app)];
        Config = UmpireConfig.Find(blocks.Current.Folder);
        Check = ArchitectureCheck.Load(
            definition, [.. dlls.Select(d => d.Path)], BinIndex.Sources(dlls), blocks, RuleConfig.Of(RuleKeys.Of(Config)), [], null,
            FolderAliasConfig.Of(Config, blocks.Current.Folder));
    }

    public Architecture Definition { get; }
    public BlockTree Blocks { get; }
    public IReadOnlyList<BinDll> App { get; }
    public IReadOnlyList<BinDll> Others { get; }
    public UmpireConfig Config { get; }
    public ArchitectureCheck Check { get; }
    public string Block => Blocks.Current.Folder;

    public static EngineRun Of(Architecture definition, string bin, string block) =>
        Of(bin, new BlockTree(new BlockEntry(block, EntryForm.App, null, block), []), (_, _) => definition);

    public static EngineRun Of(string bin, string project) {
        using var reader = new BinReader(bin);
        var index = reader.Index();
        var above = EntryFiles.Above(project);
        var folder = BlockSearch.Folder(project, "", [.. index.Entries, .. above]);
        IReadOnlyList<BlockEntry> entries = [.. index.Entries, .. above.Concat(EntryFiles.Below(folder!)).Select(reader.Check)];
        var tree = BlockSearch.Find(project, folder, entries, index).Value.Blocks;
        return Of(bin, tree, (app, loader) => LoadAppHandler.Definition(tree.Current, app, loader).Value);
    }

    static EngineRun Of(string bin, BlockTree tree, Func<IReadOnlyList<BinDll>, AppLoader, Architecture> define) {
        using var reader = new BinReader(bin);
        var index = reader.Index();
        var app = index.AppDlls(tree.Current.Folder, []);
        var definition = define(app, new AppLoader(bin));
        var externals = new ResolvedArchitecture(definition).Parts.Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        return new EngineRun(
            definition, tree, app, [.. app.Concat(index.ExternalDlls(externals)).Concat(index.ReferencedDlls(app, [])).Distinct()]);
    }
}
