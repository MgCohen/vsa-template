using VSA.Architecture;

namespace VSA.Architecture.Vsa;

public class VsaDefinition : ArchitectureDefinition {
    readonly Action<Map>? appRules;

    public VsaDefinition() { }

    public VsaDefinition(Action<Map> appRules) {
        this.appRules = appRules;
    }

    protected VsaDefinition(string root) : base(root) { }

    public const string DefaultUnitySource = "Source";

    public string? UnityApp { get; init; }

    public string UnitySource { get; init; } = DefaultUnitySource;

    public override IReadOnlyList<FolderMap> Folders(string appRoot) {
        if (!UnityLayout.Is(appRoot)) {
            return [];
        }
        var source = new FolderMap(UnitySource, "");
        if ((UnityApp ?? FindUnityApp(appRoot)) is not { } app) {
            return [source];
        }
        var appFolder = $"{UnityLayout.AssetsFolder}/{app}";
        return [new FolderMap(appFolder, ""), new FolderMap(appFolder, AppFolder), source];
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
        options.Own = Own.None;
        options.SameLayer = Permission.Allow;
        options.Isolation = Isolation.Isolated;
    }

    protected override void Define(Root root) {
        var core = root.Add<Framework>().Add<Core>();
        core.Add<Domain>().And<Infrastructure>().And<Presenters>();
        core.Add<Contracts>().Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();
        core.Add<Handlers>().Add<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>();
        core.Add<Views>();
        core.Add<Composition>().And<Host>();

        root.Add<App>().Add<Host>().And<Views>();

        var plugin = root.AddMany<Plugin>(Isolation.Open);
        plugin.Add<RootClass>().NoFolder().OfType<VSA.Module.Plugin>();
        plugin.Add<Domain>().Add<Domain.Shared>();
        plugin.Add<Infrastructure>().And<Presenters>();
        plugin.Add<Contracts>().Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();
        plugin.Add<Handlers>().Add<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>();

        var feature = root.AddMany<Feature>();
        feature.Add<RootClass>().NoFolder().OfType<VSA.Module.Module>();
        feature.Add<Domain>().Add<Domain.Shared>();
        feature.Add<Infrastructure>().And<Presenters>();
        feature.AddMany<Slice>()
            .Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>()
            .And<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>()
            .NoFolder();
    }

    protected override void Rules(Map map) {
        map.For<Domain>().Uses<Domain.Shared>();
        map.For<Infrastructure>().Uses<Domain>();
        map.For<Presenters>().Uses<Contracts>()
            .And<Contracts.Requests>(Access.Internal).And<Contracts.Events>(Access.Internal).And<Domain.Shared>(Access.Internal);
        map.For<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>()
            .Uses<Contracts>().And<Domain.Shared>();
        map.For<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>()
            .Uses<Handlers>().And<Contracts>().And<Contracts.Commands>().And<Contracts.Events>()
            .And<Domain>(Access.Internal).And<Infrastructure>(Access.Internal);
        map.For<Handlers.Requests>().Uses<Contracts.Requests>();

        map.For<Plugin>().Denies<Feature>();
        map.For<Domain>().Under<Plugin>().Denies<Domain>(Reach.Only).InOther<Plugin>();

        map.For<Feature>().Uses<Domain.Shared>().InOther<Feature>();
        map.For<Presenters>().And<RootClass>().Under<Feature>().Uses<Domain.Shared>(Access.Internal).InOther<Feature>();
        map.For<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>().Under<Feature>()
            .Uses<Contracts.Commands>(Isolation.Open).And<Contracts.Events>(Isolation.Open);

        map.For<RootClass>().Uses<Core>().And<Plugin>(Access.Internal);
        map.For<RootClass>().Denies<RootClass>().Under<Plugin>();
        map.For<RootClass>().Under<Feature>().Uses<Feature>(Access.Internal);
        map.For<Host>().Uses<Composition>();
        map.For<Views>().Uses<Presenters>();

        appRules?.Invoke(map);
    }
}
