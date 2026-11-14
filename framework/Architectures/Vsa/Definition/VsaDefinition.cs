using VSA.Architecture;

namespace VSA.Architecture.Vsa;

public class VsaDefinition : ArchitectureDefinition {
    readonly Action<RuleBook>? appRules;

    public VsaDefinition() { }

    public VsaDefinition(Action<RuleBook> appRules) {
        this.appRules = appRules;
    }

    protected VsaDefinition(string root) : base(root) { }

    public const string DefaultUnitySource = "Source";

    public string? UnityApp { get; init; }

    public string UnitySource { get; init; } = DefaultUnitySource;

    public override IReadOnlyList<FolderAlias> FolderAliases(string appRoot) {
        if (!UnityLayout.Is(appRoot)) {
            return [];
        }
        var source = new FolderAlias(UnitySource, "");
        if ((UnityApp ?? FindUnityApp(appRoot)) is not { } app) {
            return [source];
        }
        var appFolder = $"{UnityLayout.AssetsFolder}/{app}";
        return [new FolderAlias(appFolder, ""), new FolderAlias(appFolder, AppFolder), source];
    }

    static readonly string AppFolder = new App().Folder;

    static readonly string[] UnityAppMarkers = [new Feature().GroupFolder!, new Plugin().GroupFolder!, new Views().Folder];

    static string? FindUnityApp(string appRoot) {
        var assets = Path.Combine(appRoot, UnityLayout.AssetsFolder);
        if (!Directory.Exists(assets)) {
            return null;
        }
        var found = Directory.GetDirectories(assets)
            .Where(folder => UnityAppMarkers.Any(marker => Directory.Exists(Path.Combine(folder, marker))))
            .Select(Path.GetFileName)
            .Where(name => !string.Equals(name, UnityLayout.PluginsFolder, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        return found.Count switch {
            0 => null,
            1 => found[0],
            _ => throw new InvalidOperationException(
                $"The Unity project has {found.Count} app folders in Assets: {string.Join(", ", found)}. "
                + "Set UnityApp in the definition of the app: override Architecture in the app class "
                + "and return new VsaDefinition(Rules) { UnityApp = \"<name>\" }."),
        };
    }

    protected override void Configure(Options options) {
        options.UseSameConcept = true;
        options.Boundary = Boundary.Closed;
    }

    protected override void Define(Root root) {
        var core = root.Add<Framework>().Add<Core>();
        core.Add<Domain>().And<Infrastructure>().And<Presenters>();
        core.Add<Contracts>().Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();
        core.Add<Handlers>().Add<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>();
        core.Add<Views>();
        core.Add<Composition>().And<Host>();

        root.Add<App>().Add<Host>().And<Views>();

        var plugin = root.AddGroup<Plugin>(Boundary.Open);
        plugin.Add<Module>().OfType<VSA.Module.Plugin>();
        plugin.Add<Domain>().Add<Domain.Shared>();
        plugin.Add<Infrastructure>().And<Presenters>();
        plugin.Add<Contracts>().Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();
        plugin.Add<Handlers>().Add<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>();

        var feature = root.AddGroup<Feature>();
        feature.Add<Module>().OfType<VSA.Module.Module>();
        feature.Add<Domain>().Add<Domain.Shared>();
        feature.Add<Infrastructure>().And<Presenters>();
        feature.AddGroup<Slice>()
            .Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>()
            .And<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>()
            .InParentFolder();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Domain>().Uses<Domain.Shared>();
        rules.For<Infrastructure>().Uses<Domain>();
        rules.For<Infrastructure>().Uses<Handlers>(Depth.Self).And<Contracts>(Depth.Self);
        rules.For<Presenters>().Uses<Contracts>()
            .And<Contracts.Requests>(Access.Internal).And<Contracts.Events>(Access.Internal).And<Domain.Shared>(Access.Internal);
        rules.For<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>()
            .Uses<Contracts>().And<Domain.Shared>();
        rules.For<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>()
            .Uses<Handlers>().And<Contracts>().And<Contracts.Commands>().And<Contracts.Events>()
            .And<Domain>(Access.Internal).And<Infrastructure>(Access.Internal);
        rules.For<Handlers.Requests>().Uses<Contracts.Requests>();

        rules.For<Plugin>().Denies<Feature>();
        rules.For<Domain>().Under<Plugin>().Denies<Domain>(Depth.Self).InOther<Plugin>();

        rules.For<Feature>().Uses<Domain.Shared>().InOther<Feature>();
        rules.For<Presenters>().And<Module>().Under<Feature>().Uses<Domain.Shared>(Access.Internal).InOther<Feature>();
        rules.For<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>().Under<Feature>()
            .Uses<Contracts.Commands>(Boundary.Open).And<Contracts.Events>(Boundary.Open);

        rules.For<Module>().Uses<Core>().And<Plugin>(Access.Internal);
        rules.For<Module>().Denies<Module>().Under<Plugin>();
        rules.For<Module>().Under<Feature>().Uses<Feature>(Access.Internal);
        rules.For<Host>().Uses<Composition>().And<Core>(Depth.Self);
        rules.For<Views>().Uses<Presenters>();

        appRules?.Invoke(rules);
    }
}
