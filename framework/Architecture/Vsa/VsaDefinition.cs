using VSA.Architecture;

namespace VSA.Architecture.Vsa;

public class VsaDefinition : ArchitectureDefinition {
    readonly Action<Map>? appRules;

    public VsaDefinition() { }

    public VsaDefinition(Action<Map> appRules) {
        this.appRules = appRules;
    }

    protected VsaDefinition(string root) : base(root) { }

    protected override void Configure(Options options) {
        options.Unlisted = Permission.Deny;
        options.Own = Own.None;
        options.Reach = Reach.WithChildren;
    }

    protected override void Define(Root root) {
        var core = root.Add<Framework>().Add<Core>();
        core.Add<Domain>().And<Infrastructure>().And<Presenters>();
        core.Add<Contracts>().Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();
        core.Add<Handlers>().Add<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>();
        core.Add<Views>();

        root.Add<App>().Add<Host>().And<Views>();

        var plugin = root.AddMany<Plugin>();
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
        UsesOfEachLayer(map);
        Borders(map);
        ExceptionsToTheBorders(map);
        ModuleRootsAndTheApp(map);
        appRules?.Invoke(map);
    }

    static void UsesOfEachLayer(Map map) {
        map.For<Domain>().Uses<Domain>();
        map.For<Domain.Shared>().Denies<Domain>();
        map.For<Domain.Shared>().Uses<Domain.Shared>();
        map.For<Infrastructure>().Uses<Domain>().And<Infrastructure>();
        map.For<Presenters>().Uses<Presenters>().And<Contracts.Requests>(Access.Internal).And<Contracts.Events>(Access.Internal).And<Domain.Shared>();
        map.For<Contracts.Requests>().Uses<Contracts.Requests>().And<Domain.Shared>();
        map.For<Contracts.Events>().Uses<Contracts.Events>().And<Domain.Shared>();
        map.For<Contracts.Commands>().Uses<Contracts.Commands>().And<Domain.Shared>();
        map.For<Handlers.Requests>().Uses<Handlers.Requests>()
            .And<Contracts.Requests>().And<Contracts.Commands>().And<Contracts.Events>().And<Domain>(Access.Internal).And<Infrastructure>(Access.Internal);
        map.For<Handlers.Events>().Uses<Handlers.Events>()
            .And<Contracts.Commands>().And<Contracts.Events>().And<Domain>(Access.Internal).And<Infrastructure>(Access.Internal);
        map.For<Handlers.Commands>().Uses<Handlers.Commands>()
            .And<Contracts.Commands>().And<Contracts.Events>().And<Domain>(Access.Internal).And<Infrastructure>(Access.Internal);

        map.For<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>().Uses<Contracts>();
        map.For<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>().Uses<Handlers>().And<Contracts>();
        map.For<Presenters>().Uses<Contracts>();
    }

    static void Borders(Map map) {
        map.For<Core>().Denies<Plugin>().And<Feature>();
        map.For<Plugin>().Denies<Feature>();
        map.For<Feature>().Denies<Feature>().InOther<Feature>();
        map.For<Slice>().Denies<Slice>().InOther<Slice>();
        map.For<Domain>().Under<Plugin>().Denies<Domain>().InOther<Plugin>();
    }

    static void ExceptionsToTheBorders(Map map) {
        map.For<Feature>().Uses<Domain.Shared>().InOther<Feature>();
        map.For<Domain>().Under<Plugin>().Uses<Domain.Shared>().InOther<Plugin>();
        map.For<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>().Under<Feature>()
            .Uses<Contracts.Commands>().And<Contracts.Events>().InOther<Feature>();
        map.For<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>().Under<Feature>()
            .Uses<Contracts.Events>().InOther<Slice>();
    }

    static void ModuleRootsAndTheApp(Map map) {
        map.For<RootClass>().Uses<Core>().And<Plugin>();
        map.For<RootClass>().Under<Plugin>().Uses<Plugin>(Access.Internal).InOwn<Plugin>();
        map.For<RootClass>().Denies<RootClass>().Under<Plugin>();
        map.For<RootClass>().Under<Plugin>().Uses<RootClass>().InOwn<Plugin>();
        map.For<RootClass>().Under<Feature>().Uses<Feature>(Access.Internal).InOwn<Feature>();
        map.For<Host>().Under<App>().Uses<RootClass>().And<Core>();
        map.For<Views>().Under<App>().Uses<Presenters>().And<Views>();
    }
}
