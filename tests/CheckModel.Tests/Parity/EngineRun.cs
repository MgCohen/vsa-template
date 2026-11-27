namespace CheckModel.Tests;

using Blocks.Domain.Shared;
using Blocks.Infrastructure;
using Blocks.Slices.FindBlocks;
using Facts.Domain.Shared;
using Facts.Infrastructure;
using LoadApp.Slices.LoadApp;
using Rules.Domain;
using Rules.Slices.CheckRules;
using Operations.Domain.Shared;
using Umpire.Architecture;
using Umpire.Config;

sealed class EngineRun {
    EngineRun(
        Architecture definition, BlockTree blocks, IReadOnlyList<BinDll> app, IReadOnlyList<BinDll> dlls,
        IReadOnlyList<string> ignored, IReadOnlyList<FolderAlias> aliases) {
        Definition = definition;
        Blocks = blocks;
        App = app;
        Others = [.. dlls.Except(app)];
        Sources = BinIndex.Sources(dlls);
        IgnoredFolders = ignored;
        Config = UmpireConfig.Find(blocks.Current.Folder);
        var configured = FolderAliasConfig.Of(Config, blocks.Current.Folder);
        Aliases = [.. configured.Aliases, .. aliases];
        Check = ArchitectureCheck.Load(
            new TypeFactSource(), definition, [.. dlls.Select(d => d.Path)], Sources, blocks, RuleConfig.Of(RuleKeys.Of(Config)), ignored, null,
            configured with { Aliases = Aliases });
    }

    public Architecture Definition { get; }
    public BlockTree Blocks { get; }
    public IReadOnlyList<BinDll> App { get; }
    public IReadOnlyList<BinDll> Others { get; }
    public IReadOnlyDictionary<string, Source> Sources { get; }
    public IReadOnlyList<string> IgnoredFolders { get; }
    public IReadOnlyList<FolderAlias> Aliases { get; }
    public UmpireConfig Config { get; }
    public ArchitectureCheck Check { get; }
    public string Block => Blocks.Current.Folder;

    public static EngineRun Of(
        Architecture definition, string bin, string block, IReadOnlyList<string>? ignored = null, IReadOnlyList<FolderAlias>? aliases = null) =>
        Of(bin, new BlockTree(new BlockEntry(block, EntryForm.App, null, block), []), (_, _) => definition, ignored ?? [], aliases ?? []);

    public static EngineRun Of(string bin, string project) {
        using var reader = new BinReader(bin);
        var index = reader.Index();
        var above = EntryFiles.Above(project);
        var folder = BlockSearch.Folder(project, "", [.. index.Entries, .. above]);
        IReadOnlyList<BlockEntry> entries = [.. index.Entries, .. above.Concat(EntryFiles.Below(folder!)).Select(reader.Check)];
        var tree = BlockSearch.Find(project, folder, entries, index).Value.Blocks;
        var hasSource = DefinitionFolder.Of(tree.Current.Folder).HasSource;
        return Of(bin, tree, (app, loader) => LoadAppHandler.Definition(tree, app, index.Definitions, loader, hasSource).Value, [], []);
    }

    static EngineRun Of(
        string bin, BlockTree tree, Func<IReadOnlyList<BinDll>, AppLoader, Architecture> define,
        IReadOnlyList<string> ignored, IReadOnlyList<FolderAlias> aliases) {
        using var reader = new BinReader(bin, new ReadOptions([], null, ignored));
        var index = reader.Index();
        var app = index.AppDlls(tree.Current.Folder, []);
        var definition = define(app, new AppLoader(bin));
        var externals = new ResolvedArchitecture(definition).Parts.Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        return new EngineRun(
            definition, tree, app, [.. app.Concat(index.ExternalDlls(externals)).Concat(index.ReferencedDlls(app, [])).Distinct()],
            ignored, aliases);
    }
}
