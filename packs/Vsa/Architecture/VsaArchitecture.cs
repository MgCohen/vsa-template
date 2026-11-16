namespace Umpire.Vsa.Definition;

using Umpire.Architecture;

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
        kernel.Add<Domain>().InAssembly("Umpire.Vsa.Domain");
        kernel.Add<Infrastructure>().InAssembly("Umpire.Vsa.Infrastructure");
        kernel.Add<Presenters>().InAssembly("Umpire.Vsa.Presenters");
        var contracts = kernel.Add<Contracts>().InAssembly("Umpire.Messages.Contracts");
        contracts.Add<Contracts.Requests>().InAssembly("Umpire.Vsa.Contracts.Requests");
        contracts.Add<Contracts.Events>().InAssembly("Umpire.Vsa.Contracts.Events");
        contracts.Add<Contracts.Commands>().InAssembly("Umpire.Vsa.Contracts.Commands");
        var handlers = kernel.Add<Handlers>().InAssembly("Umpire.Messages.Handlers");
        handlers.Add<Handlers.Requests>().InAssembly("Umpire.Vsa.Handlers.Requests");
        handlers.Add<Handlers.Events>().InAssembly("Umpire.Vsa.Handlers.Events");
        handlers.Add<Handlers.Commands>().InAssembly("Umpire.Vsa.Handlers.Commands");
        kernel.Add<Views>().InAssembly("Umpire.Vsa.Views");
        kernel.Add<Composition>().InAssembly("Umpire.Vsa.Composition");
        kernel.Add<Host>().InAssembly("Umpire.Vsa.Hosts.Web");

        root.Add<App>().Add<Host>().And<Views>();

        var plugin = root.AddGroup<Plugin>(Boundary.Open);
        plugin.Add<Module>().OfType<Umpire.Vsa.Composition.Plugin>();
        plugin.Add<Domain>().Add<Domain.Shared>();
        plugin.Add<Infrastructure>().And<Presenters>();
        plugin.Add<Contracts>().Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();
        var pluginHandlers = plugin.Add<Handlers>();
        pluginHandlers.Add<Handlers.Requests>();
        pluginHandlers.Add<Handlers.Events>().Add<Handlers.Events.Hooks>().InParentFolder();
        pluginHandlers.Add<Handlers.Commands>().Add<Handlers.Commands.Hooks>().InParentFolder();

        var feature = root.AddGroup<Feature>();
        feature.Add<Module>().OfType<Umpire.Kernel.Module>();
        feature.Add<Domain>().Add<Domain.Shared>();
        feature.Add<Infrastructure>().And<Presenters>();
        feature.Add<Slice.Shared>();
        feature.AddGroup<Slice>()
            .Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>()
            .And<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>()
            .And<Handlers.Events.Hooks>().And<Handlers.Commands.Hooks>()
            .InParentFolder();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Domain>().Uses<Domain.Shared>();
        rules.For<Infrastructure>().Uses<Domain>();
        rules.For<Infrastructure>().Uses<Handlers>(Depth.Self).And<Contracts>(Depth.Self);
        rules.For<Presenters>().Uses<Contracts>()
            .And<Contracts.Requests>(Access.Internal).And<Contracts.Events>(Access.Internal).And<Domain.Shared>(Access.Internal)
            .And<Slice.Shared>(Access.Internal);
        rules.For<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>().And<Slice.Shared>()
            .Uses<Contracts>().And<Domain.Shared>().And<Slice.Shared>();
        rules.For<Slice.Shared>().Uses<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>().Under<Kernel>();
        rules.For<Slice.Shared>().Uses<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>().Under<Plugin>();
        rules.For<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>()
            .And<Handlers.Events.Hooks>().And<Handlers.Commands.Hooks>()
            .Uses<Handlers>().And<Contracts>().And<Contracts.Commands>().And<Contracts.Events>().And<Slice.Shared>()
            .And<Domain>(Access.Internal).And<Infrastructure>(Access.Internal);
        rules.For<Handlers.Requests>().Uses<Contracts.Requests>();
        rules.For<Handlers.Events.Hooks>().Uses<Handlers.Events>();
        rules.For<Handlers.Commands.Hooks>().Uses<Handlers.Commands>();

        rules.For<Plugin>().Denies<Feature>();
        rules.For<Domain>().Under<Plugin>().Denies<Domain>(Depth.Self).InOther<Plugin>();

        rules.For<Feature>().Uses<Domain.Shared>().InOther<Feature>();
        rules.For<Presenters>().And<Module>().Under<Feature>().Uses<Domain.Shared>(Access.Internal).InOther<Feature>();
        rules.For<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>()
            .And<Handlers.Events.Hooks>().And<Handlers.Commands.Hooks>().Under<Feature>()
            .Uses<Contracts.Commands>(Boundary.Open).And<Contracts.Events>(Boundary.Open);

        rules.For<Module>().Uses<Kernel>().And<Plugin>(Access.Internal);
        rules.For<Module>().Denies<Module>().Under<Plugin>();
        rules.For<Module>().Under<Feature>().Uses<Feature>(Access.Internal);
        rules.For<Module>().Under<Feature>().Denies<Composition>();
        rules.For<Host>().Uses<Composition>().And<Kernel>(Depth.Self);
        rules.For<Views>().Uses<Presenters>();

        rules.For<Slice>().Has<Handlers>().AtLeast(1);
        rules.For<Slice>().Has<Handlers>().OfOneKind();
        rules.For<Handlers.Requests>().Under<Feature>().Takes<Contracts.Requests>().InSame<Slice>();
        rules.For<Contracts.Requests>().Under<Feature>().Has<Handlers.Requests>(Depth.Self).Exactly(1);
        rules.For<Handlers.Commands>().Under<Feature>().Takes<Contracts.Commands>().InSame<Slice>();
        rules.For<Contracts.Commands>().Under<Feature>().Has<Handlers.Commands>(Depth.Self).Exactly(1);
        rules.For<Feature>().Has<Module>().Exactly(1);
        rules.For<Plugin>().Has<Module>().Exactly(1);
    }
}
