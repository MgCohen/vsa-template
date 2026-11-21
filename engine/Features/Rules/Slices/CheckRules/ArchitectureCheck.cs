namespace Rules.Slices.CheckRules;

using Blocks.Domain.Shared;
using BuiltApp.Infrastructure;
using Rules.Domain;
using Steps.Domain.Shared;
using Umpire.Architecture;

sealed class ArchitectureCheck {
    readonly RuleConfig config;

    ArchitectureCheck(
        ResolvedArchitecture resolved, LoadedApp app, Bindings bindings, IReadOnlyList<string> innerFolders, RuleConfig config) {
        Resolved = resolved;
        App = app;
        Bindings = bindings;
        InnerFolders = innerFolders;
        this.config = config;
    }

    public ResolvedArchitecture Resolved { get; }
    public LoadedApp App { get; }
    public Bindings Bindings { get; }
    public IReadOnlyList<string> InnerFolders { get; }
    public IEnumerable<Binding> CheckedBindings => Bindings.All.Where(b => !b.External && !b.Part.IsBlockPlace);

    public static ArchitectureCheck Load(
        Architecture definition, IReadOnlyList<string> dlls, IReadOnlyDictionary<string, Source> sources, BlockTree blocks) =>
        Load(definition, dlls, sources, blocks, RuleConfig.Empty);

    public static ArchitectureCheck Load(
        Architecture definition, IReadOnlyList<string> dlls, IReadOnlyDictionary<string, Source> sources, BlockTree blocks,
        RuleConfig config, IReadOnlyList<string>? ignoredFolders = null) {
        var resolved = new ResolvedArchitecture(definition);
        var app = AppReader.Read(dlls, sources, blocks.Current.Folder, resolved, ignoredFolders);
        var bindings = resolved.Place(app.Facts, app.BlockFolder, blocks.InnerFolders);
        return new ArchitectureCheck(resolved, app, bindings, blocks.InnerFolders, config);
    }

    public IReadOnlyList<Finding> Findings() => [.. ArchitectureRule.All.SelectMany(rule => rule.Check(this, config))];
}
