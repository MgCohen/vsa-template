namespace Umpire.Vsa.Definition;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;
using Umpire.Kernels.Blocks;
using Core = Umpire.Kernels.Blocks.Core;
using CoreArchitecture = Umpire.Kernels.CoreArchitecture;
using MessagesComponent = Umpire.Kernels.MessagesComponent;
using MessagesParts = Umpire.Kernels.MessagesParts;

public class VsaArchitecture : CoreArchitecture {
    protected override void Configure(ArchitectureConfig config) {
        config.Defaults.UseSameConcept = true;
        config.Defaults.Boundary = Boundary.Closed;
        config.Roots.Add<Views>();
    }

    protected override void Define(Part root, Part kernel) {
        kernel.Add<Domain>().And<Infrastructure>().And<Presenters>();
        kernel.Add<Contracts>().Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();
        kernel.Add<Handlers>().Add<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>();
        kernel.Add<Views>().And<Composition>();
        root.AddBlock<MessagesComponent>();
        root.Add<Host>();
        root.AddBlockGroup<Pack>("Packs");
        root.AddGroup<Plugin>(Boundary.Open);
        root.AddGroup<Feature>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Kernel>().Uses<Kernel>();
        rules.For<Domain>().Uses<Domain.Shared>();
        rules.For<Infrastructure>().Uses<Domain>();
        rules.For<Infrastructure>().Uses<Handlers>(Depth.Self).And<Contracts>(Depth.Self)
            .And<MessagesParts.Handlers>().And<MessagesParts.Contracts>();
        rules.For<Contracts>().Uses<MessagesParts.Contracts>();
        rules.For<Handlers>().Uses<MessagesParts.Handlers>();
        rules.For<Presenters>().Uses<Contracts>().And<MessagesParts.Contracts>()
            .And<Contracts.Requests>(Access.Internal).And<Contracts.Events>(Access.Internal).And<Domain.Shared>(Access.Internal)
            .And<Slice.Shared>(Access.Internal);
        rules.For<Presenters>().Uses<Contracts.Requests>().Under<Kernel>();
        ForContractParts(rules).And<Slice.Shared>()
            .Uses<Contracts>().And<MessagesParts.Contracts>().And<Domain.Shared>().And<Slice.Shared>();
        UsesContractParts(rules.For<Slice.Shared>()).Under<Kernel>();
        UsesContractParts(rules.For<Slice.Shared>()).Under<Plugin>();
        ForHandlerParts(rules)
            .Uses<Handlers>().And<Contracts>().And<MessagesParts.Handlers>().And<MessagesParts.Contracts>()
            .And<Contracts.Commands>().And<Contracts.Events>().And<Slice.Shared>()
            .And<Domain>(Access.Internal).And<Infrastructure>(Access.Internal);
        rules.For<Handlers.Requests>().Uses<Contracts.Requests>();
        rules.For<Handlers.Events.Hooks>().Uses<Handlers.Events>();
        rules.For<Handlers.Commands.Hooks>().Uses<Handlers.Commands>();

        rules.For<Plugin>().Denies<Feature>();
        rules.For<Domain>().Under<Plugin>().Denies<Domain>(Depth.Self).InOther<Plugin>();

        rules.For<Feature>().Uses<Domain.Shared>().InOther<Feature>();
        rules.For<Presenters>().And<Feature>(Depth.Self).Under<Feature>().Uses<Domain.Shared>(Access.Internal).InOther<Feature>();
        ForHandlerParts(rules).Under<Feature>()
            .Uses<Contracts.Commands>(Boundary.Open).And<Contracts.Events>(Boundary.Open);

        rules.For<Module>(Depth.Self).Uses<Kernel>().And<MessagesComponent>().And<UmpireLibraries>().And<Plugin>(Access.Internal);
        rules.For<Module>(Depth.Self).Denies<Plugin>(Depth.Self);
        rules.For<Feature>(Depth.Self).Uses<Feature>(Access.Internal);
        rules.For<Feature>(Depth.Self).Denies<Composition>();
        rules.For<Host>().Uses<Composition>().And<Core>().And<UmpireLibraries>();
        rules.For<Views>().Uses<Presenters>();
        rules.For<Views>().Uses<Hosts>().Under<Kernel>();
        rules.For<Host>().Uses<Pack>();
        rules.For<Host>().Uses<Module>(Depth.Self);
        rules.For<Feature>().And<Plugin>().Uses<Pack>();

    }

    protected static From ForContractParts(RuleBook rules) =>
        rules.For<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();

    protected static Rule UsesContractParts(From from) =>
        from.Uses<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();

    protected static From ForHandlerParts(RuleBook rules) =>
        rules.For<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>()
            .And<Handlers.Events.Hooks>().And<Handlers.Commands.Hooks>();
}
