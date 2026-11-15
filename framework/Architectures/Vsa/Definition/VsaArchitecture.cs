using VSA.Architecture;

namespace VSA.Architecture.Vsa;

public class VsaArchitecture : Architecture {
    public VsaArchitecture() { }

    protected VsaArchitecture(string root) : base(root) { }

    public const string DefaultUnitySource = "Source";

    public string? UnityApp { get; init; }

    public string UnitySource { get; init; } = DefaultUnitySource;

    public override IReadOnlyList<FolderAlias> FolderAliases(string appRoot) => UnityLayout.Aliases(appRoot, UnityApp, UnitySource);

    protected override void Configure(Options defaults) {
        defaults.UseSameConcept = true;
        defaults.Boundary = Boundary.Closed;
    }

    protected override void Define(Root root) {
        var kernel = root.Add<Framework>().Add<Kernel>();
        kernel.Add<Domain>().And<Infrastructure>().And<Presenters>();
        kernel.Add<Contracts>().Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();
        kernel.Add<Handlers>().Add<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>();
        kernel.Add<Views>();
        kernel.Add<Composition>().And<Host>();

        root.Add<App>().Add<Host>().And<Views>();

        var plugin = root.AddGroup<Plugin>(Boundary.Open);
        plugin.Add<Module>().OfType<VSA.Kernel.Plugin>();
        plugin.Add<Domain>().Add<Domain.Shared>();
        plugin.Add<Infrastructure>().And<Presenters>();
        plugin.Add<Contracts>().Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();
        plugin.Add<Handlers>().Add<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>();

        var feature = root.AddGroup<Feature>();
        feature.Add<Module>().OfType<VSA.Kernel.Module>();
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

        rules.For<Module>().Uses<Kernel>().And<Plugin>(Access.Internal);
        rules.For<Module>().Denies<Module>().Under<Plugin>();
        rules.For<Module>().Under<Feature>().Uses<Feature>(Access.Internal);
        rules.For<Host>().Uses<Composition>().And<Kernel>(Depth.Self);
        rules.For<Views>().Uses<Presenters>();
    }
}
