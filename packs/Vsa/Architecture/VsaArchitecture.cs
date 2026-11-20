namespace Umpire.Vsa.Definition;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public class VsaArchitecture : Architecture {
    public const string DefaultUnitySource = "Source";

    public string? UnityApp { get; init; }

    public string UnitySource { get; init; } = DefaultUnitySource;

    public override IReadOnlyList<FolderAlias> FolderAliases(string blockFolder) => UnityLayout.Aliases(blockFolder, UnityApp, UnitySource);

    protected override void Configure(Options defaults) {
        defaults.UseSameConcept = true;
        defaults.Boundary = Boundary.Closed;
    }

    protected override void Define(Block block) {
        DefineKernel(block.Add<Kernel>());
        block.Add<App>().Add<Host>().And<Views>();
        block.AddBlockGroup<Pack>("Packs");
        DefinePlugin(block.AddGroup<Plugin>(Boundary.Open));
        DefineFeature(block.AddGroup<Feature>());
    }

    protected override void DefineKernel(Part kernel) {
        Bind(kernel.Add<Domain>(), "Umpire.Vsa.Domain");
        Bind(kernel.Add<Infrastructure>(), "Umpire.Vsa.Infrastructure");
        Bind(kernel.Add<Presenters>(), "Umpire.Vsa.Presenters");
        var contracts = Bind(kernel.Add<Contracts>(), "Umpire.Messages.Contracts");
        Bind(contracts.Add<Contracts.Requests>(), "Umpire.Vsa.Contracts.Requests");
        Bind(contracts.Add<Contracts.Events>(), "Umpire.Vsa.Contracts.Events");
        Bind(contracts.Add<Contracts.Commands>(), "Umpire.Vsa.Contracts.Commands");
        var handlers = Bind(kernel.Add<Handlers>(), "Umpire.Messages.Handlers");
        Bind(handlers.Add<Handlers.Requests>(), "Umpire.Vsa.Handlers.Requests");
        Bind(handlers.Add<Handlers.Events>(), "Umpire.Vsa.Handlers.Events");
        Bind(handlers.Add<Handlers.Commands>(), "Umpire.Vsa.Handlers.Commands");
        Bind(kernel.Add<Views>(), "Umpire.Vsa.Views");
        Bind(kernel.Add<Composition>(), "Umpire.Vsa.Composition");
        if (kernel.IsExternal) {
            kernel.Add<Host>().InAssembly("Umpire.Hosts");
        }
    }

    protected override void KernelRules(RuleBook rules) {
        UsesKernelLayers(ForKernelLayers(rules));
    }

    protected virtual void DefinePlugin(PartGroup plugin) {
        plugin.Add<Module>().OfType<Umpire.Vsa.Composition.Plugin>();
        plugin.Add<Domain>().Add<Domain.Shared>();
        plugin.Add<Infrastructure>().And<Presenters>();
        AddContractParts(plugin.Add<Contracts>());
        var pluginHandlers = plugin.Add<Handlers>();
        pluginHandlers.Add<Handlers.Requests>();
        pluginHandlers.Add<Handlers.Events>().Add<Handlers.Events.Hooks>().InParentFolder();
        pluginHandlers.Add<Handlers.Commands>().Add<Handlers.Commands.Hooks>().InParentFolder();
    }

    protected virtual void DefineFeature(PartGroup feature) {
        feature.Add<Module>().OfType<Umpire.Kernels.Module>();
        feature.Add<Domain>().Add<Domain.Shared>();
        feature.Add<Infrastructure>().And<Presenters>();
        feature.Add<Slice.Shared>();
        DefineSlice(feature.AddGroup<Slice>());
    }

    protected virtual void DefineSlice(PartGroup slice) { }

    protected override void Rules(RuleBook rules) {
        rules.For<Domain>().Uses<Domain.Shared>();
        rules.For<Infrastructure>().Uses<Domain>();
        rules.For<Infrastructure>().Uses<Handlers>(Depth.Self).And<Contracts>(Depth.Self);
        rules.For<Presenters>().Uses<Contracts>()
            .And<Contracts.Requests>(Access.Internal).And<Contracts.Events>(Access.Internal).And<Domain.Shared>(Access.Internal)
            .And<Slice.Shared>(Access.Internal);
        rules.For<Presenters>().Uses<Contracts.Requests>().Under<Kernel>();
        ForContractParts(rules).And<Slice.Shared>()
            .Uses<Contracts>().And<Domain.Shared>().And<Slice.Shared>();
        UsesContractParts(rules.For<Slice.Shared>()).Under<Kernel>();
        UsesContractParts(rules.For<Slice.Shared>()).Under<Plugin>();
        ForHandlerParts(rules)
            .Uses<Handlers>().And<Contracts>().And<Contracts.Commands>().And<Contracts.Events>().And<Slice.Shared>()
            .And<Domain>(Access.Internal).And<Infrastructure>(Access.Internal);
        rules.For<Handlers.Requests>().Uses<Contracts.Requests>();
        rules.For<Handlers.Events.Hooks>().Uses<Handlers.Events>();
        rules.For<Handlers.Commands.Hooks>().Uses<Handlers.Commands>();

        rules.For<Plugin>().Denies<Feature>();
        rules.For<Domain>().Under<Plugin>().Denies<Domain>(Depth.Self).InOther<Plugin>();

        rules.For<Feature>().Uses<Domain.Shared>().InOther<Feature>();
        rules.For<Presenters>().And<Module>().Under<Feature>().Uses<Domain.Shared>(Access.Internal).InOther<Feature>();
        ForHandlerParts(rules).Under<Feature>()
            .Uses<Contracts.Commands>(Boundary.Open).And<Contracts.Events>(Boundary.Open);

        rules.For<Module>().Uses<Kernel>().And<Plugin>(Access.Internal);
        rules.For<Module>().Denies<Module>().Under<Plugin>();
        rules.For<Module>().Under<Feature>().Uses<Feature>(Access.Internal);
        rules.For<Module>().Under<Feature>().Denies<Composition>();
        rules.For<Host>().Uses<Composition>().And<Kernel>(Depth.Self);
        rules.For<Views>().Uses<Presenters>();
        rules.For<Views>().Uses<Host>().Under<Kernel>();
        rules.For<Host>().Under<App>().Uses<Pack>();
        rules.For<Feature>().And<Plugin>().Uses<Pack>();

        rules.For<Feature>().Has<Module>().Exactly(1);
        rules.For<Plugin>().Has<Module>().Exactly(1);
    }
    static From ForKernelLayers(RuleBook rules) =>
        rules.For<Domain>().And<Infrastructure>().And<Presenters>().And<Contracts>().And<Contracts.Requests>()
            .And<Contracts.Events>().And<Contracts.Commands>().And<Handlers>().And<Handlers.Requests>().And<Handlers.Events>()
            .And<Handlers.Commands>().And<Views>().And<Composition>();

    static Rule UsesKernelLayers(From from) =>
        from.Uses<Domain>().And<Infrastructure>().And<Presenters>().And<Contracts>().And<Contracts.Requests>()
            .And<Contracts.Events>().And<Contracts.Commands>().And<Handlers>().And<Handlers.Requests>().And<Handlers.Events>()
            .And<Handlers.Commands>().And<Views>().And<Composition>();

    static Part Bind(Part part, string assembly) => part.IsExternal ? part.InAssembly(assembly) : part;

    protected static PartGroup AddContractParts(IPartChain parent) =>
        parent.Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();

    protected static From ForContractParts(RuleBook rules) =>
        rules.For<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();

    protected static Rule UsesContractParts(From from) =>
        from.Uses<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();

    protected static From ForHandlerParts(RuleBook rules) =>
        rules.For<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>()
            .And<Handlers.Events.Hooks>().And<Handlers.Commands.Hooks>();
}
